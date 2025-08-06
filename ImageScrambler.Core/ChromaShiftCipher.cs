using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageScrambler.Core;

public sealed class ChromaShiftCipher
{
    private const int DctBlockSize = 8;
    private readonly byte[] _passwordBytes;
    private readonly byte[] _salt;
    private readonly bool _useDctScrambling;
    private readonly string _dctPermutationContext;
    private readonly string _blocksContext;

    private readonly record struct YCbCr(byte Y, byte Cb, byte Cr);

    public ChromaShiftCipher(string password, bool useDctScrambling = true,
        string? saltContext = null, string? saltDerivationKey = null, string? saltDerivationPattern = null,
        string? dctPermutationContext = null, string? blocksContext = null)
    {
        _passwordBytes = Encoding.UTF8.GetBytes(password);
        _useDctScrambling = useDctScrambling;

        // Use default constants if not provided (for performance, no validation when using defaults)
        _dctPermutationContext = dctPermutationContext ?? CryptographicConstants.DefaultDCTPermutationContext;
        _blocksContext = blocksContext ?? CryptographicConstants.DefaultBlocksContext;

        // Validate custom parameters if provided
        if (dctPermutationContext != null && string.IsNullOrWhiteSpace(dctPermutationContext))
            throw new ArgumentException("DCT permutation context cannot be null or whitespace.", nameof(dctPermutationContext));
        if (blocksContext != null && string.IsNullOrWhiteSpace(blocksContext))
            throw new ArgumentException("Blocks context cannot be null or whitespace.", nameof(blocksContext));

        _salt = CryptographicCore.DeriveSaltFromPassword(_passwordBytes, saltContext, saltDerivationKey, saltDerivationPattern);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static YCbCr RgbToYCbCr(Rgb24 rgb)
    {
        float r = rgb.R, g = rgb.G, b = rgb.B;
        byte y = (byte)Math.Clamp(0.299f * r + 0.587f * g + 0.114f * b, 0, 255);
        byte cb = (byte)Math.Clamp(-0.169f * r - 0.331f * g + 0.500f * b + 128, 0, 255);
        byte cr = (byte)Math.Clamp(0.500f * r - 0.419f * g - 0.081f * b + 128, 0, 255);
        return new YCbCr(y, cb, cr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Rgb24 YCbCrToRgb(byte y, byte cb, byte cr)
    {
        float yf = y, cbf = cb - 128, crf = cr - 128;
        return new Rgb24(
            (byte)Math.Clamp(yf + 1.402f * crf, 0, 255),
            (byte)Math.Clamp(yf - 0.344f * cbf - 0.714f * crf, 0, 255),
            (byte)Math.Clamp(yf + 1.772f * cbf, 0, 255)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessDctScrambling(byte[] channelArray, int width, int height, bool unscramble)
    {
        if (!_useDctScrambling) return;

        int paddedWidth = (width + DctBlockSize - 1) & ~(DctBlockSize - 1);
        int paddedHeight = (height + DctBlockSize - 1) & ~(DctBlockSize - 1);

        if (paddedWidth == width && paddedHeight == height)
        {
            ProcessDctScrambling_FastPath(channelArray, width, height, unscramble);
            return;
        }

        byte[] originalPadded = ArrayPool<byte>.Shared.Rent(paddedWidth * paddedHeight);
        byte[] targetPadded = ArrayPool<byte>.Shared.Rent(paddedWidth * paddedHeight);
        try
        {
            for (int y = 0; y < height; y++)
            {
                Array.Copy(channelArray, y * width, originalPadded, y * paddedWidth, width);
            }

            for (int y = 0; y < height; y++)
            {
                byte edgePixel = originalPadded[y * paddedWidth + width - 1];
                Array.Fill(originalPadded, edgePixel, y * paddedWidth + width, paddedWidth - width);
            }

            for (int y = height; y < paddedHeight; y++)
            {
                Array.Copy(originalPadded, (height - 1) * paddedWidth, originalPadded, y * paddedWidth, paddedWidth);
            }

            ScramblePaddedBuffer(originalPadded, targetPadded, paddedWidth, paddedHeight, unscramble);

            for (int y = 0; y < height; y++)
            {
                Array.Copy(targetPadded, y * paddedWidth, channelArray, y * width, width);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(originalPadded);
            ArrayPool<byte>.Shared.Return(targetPadded);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ProcessDctScrambling_FastPath(byte[] channelArray, int width, int height, bool unscramble)
    {
        byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(channelArray.Length);
        try
        {
            Array.Copy(channelArray, tempBuffer, channelArray.Length);
            ScramblePaddedBuffer(tempBuffer, channelArray, width, height, unscramble);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ScramblePaddedBuffer(byte[] source, byte[] destination, int paddedWidth, int paddedHeight, bool unscramble)
    {
        int blocksW = paddedWidth / DctBlockSize;
        int blocksH = paddedHeight / DctBlockSize;
        int totalBlocks = blocksW * blocksH;

        var seedContext = _passwordBytes.Concat(Encoding.UTF8.GetBytes(_dctPermutationContext)).ToArray();
        int seed = CryptographicCore.GenerateSecureSeed(seedContext, _salt, _blocksContext, totalBlocks);
        var random = new Random(seed);
        int[] permutation = [.. Enumerable.Range(0, totalBlocks)];
        random.Shuffle(permutation);

        if (unscramble)
        {
            int[] inverse = new int[totalBlocks];
            for (int i = 0; i < totalBlocks; i++) inverse[permutation[i]] = i;
            permutation = inverse;
        }

        Parallel.For(0, totalBlocks, i =>
        {
            int sourceBlockIndex = permutation[i];
            int targetBlockIndex = i;

            int sourceY = (sourceBlockIndex / blocksW) * DctBlockSize;
            int sourceX = (sourceBlockIndex % blocksW) * DctBlockSize;

            int targetY = (targetBlockIndex / blocksW) * DctBlockSize;
            int targetX = (targetBlockIndex % blocksW) * DctBlockSize;

            for (int row = 0; row < DctBlockSize; row++)
            {
                int sourceIndex = (sourceY + row) * paddedWidth + sourceX;
                int targetIndex = (targetY + row) * paddedWidth + targetX;
                Array.Copy(source, sourceIndex, destination, targetIndex, DctBlockSize);
            }
        });
    }

    public async Task EncodeAsync(string hiddenImagePath, string outputCarrierPath, int jpegQuality = 100)
    {
        using var hiddenImage = await Image.LoadAsync<Rgb24>(hiddenImagePath);
        int width = hiddenImage.Width, height = hiddenImage.Height;
        int totalPixels = width * height;

        byte[] yH = ArrayPool<byte>.Shared.Rent(totalPixels);
        byte[] cbH = ArrayPool<byte>.Shared.Rent(totalPixels);
        byte[] crH = ArrayPool<byte>.Shared.Rent(totalPixels);
        byte[] yCarrier = ArrayPool<byte>.Shared.Rent(totalPixels * 3);
        Rgb24[] carrierPixels = ArrayPool<Rgb24>.Shared.Rent(totalPixels * 3);

        try
        {
            var accessor = hiddenImage.GetPixelMemoryGroup();
            for (int y = 0; y < height; y++)
            {
                var rowSpan = hiddenImage.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < width; x++)
                {
                    var ycbcr = RgbToYCbCr(rowSpan[x]);
                    int index = y * width + x;
                    yH[index] = ycbcr.Y;
                    cbH[index] = ycbcr.Cb;
                    crH[index] = ycbcr.Cr;
                }
            }

            Array.Copy(yH, 0, yCarrier, 0, totalPixels);
            Array.Copy(cbH, 0, yCarrier, totalPixels, totalPixels);
            Array.Copy(crH, 0, yCarrier, totalPixels * 2, totalPixels);

            ProcessDctScrambling(yCarrier, width, height * 3, unscramble: false);

            Parallel.For(0, totalPixels * 3, i =>
            {
                carrierPixels[i] = YCbCrToRgb(yCarrier[i], 128, 128);
            });

            var carrierPixelsSpan = carrierPixels.AsSpan(0, totalPixels * 3);
            using var carrierImage = Image.LoadPixelData<Rgb24>(carrierPixelsSpan, width, height * 3);

            await carrierImage.SaveAsJpegAsync(outputCarrierPath, new JpegEncoder { Quality = jpegQuality, SkipMetadata = true });
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(yH);
            ArrayPool<byte>.Shared.Return(cbH);
            ArrayPool<byte>.Shared.Return(crH);
            ArrayPool<byte>.Shared.Return(yCarrier);
            ArrayPool<Rgb24>.Shared.Return(carrierPixels);
        }
    }

    public async Task DecodeAsync(string carrierImagePath, string outputHiddenPath, int jpegQuality = 100)
    {
        using var carrierImage = await Image.LoadAsync<Rgb24>(carrierImagePath);
        int carrierWidth = carrierImage.Width, carrierHeight = carrierImage.Height;
        if (carrierHeight % 3 != 0)
            throw new ArgumentException("Invalid carrier image: height is not divisible by 3.");

        int hiddenWidth = carrierWidth, hiddenHeight = carrierHeight / 3;
        int hiddenTotalPixels = hiddenWidth * hiddenHeight;

        byte[] yCarrier = ArrayPool<byte>.Shared.Rent(carrierWidth * carrierHeight);
        Rgb24[] hiddenPixels = ArrayPool<Rgb24>.Shared.Rent(hiddenTotalPixels);

        try
        {
            for (int y = 0; y < carrierHeight; y++)
            {
                var rowSpan = carrierImage.DangerousGetPixelRowMemory(y).Span;
                for (int x = 0; x < carrierWidth; x++)
                {
                    yCarrier[y * carrierWidth + x] = RgbToYCbCr(rowSpan[x]).Y;
                }
            }

            ProcessDctScrambling(yCarrier, hiddenWidth, carrierHeight, unscramble: true);

            Parallel.For(0, hiddenTotalPixels, i =>
            {
                byte yVal = yCarrier[i];
                byte cbVal = yCarrier[hiddenTotalPixels + i];
                byte crVal = yCarrier[hiddenTotalPixels * 2 + i];
                hiddenPixels[i] = YCbCrToRgb(yVal, cbVal, crVal);
            });

            var hiddenPixelsSpan = hiddenPixels.AsSpan(0, hiddenTotalPixels);
            using var hiddenImage = Image.LoadPixelData<Rgb24>(hiddenPixelsSpan, hiddenWidth, hiddenHeight);

            await hiddenImage.SaveAsJpegAsync(outputHiddenPath, new JpegEncoder { Quality = jpegQuality, SkipMetadata = true });
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(yCarrier);
            ArrayPool<Rgb24>.Shared.Return(hiddenPixels);
        }
    }
}