using System.Text;

using ImageScrambler.Core;

namespace ImageScrambler;

internal class Program
{
    private const string DefaultPassword = "Default@Password";

    private sealed class CommandOptions
    {
        public string Command { get; set; } = string.Empty;
        public string InputPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int Quality { get; set; } = 100;
        public bool UseDctScrambling { get; set; } = true;
        public string? SaltContext { get; set; }
        public string? SaltDerivationKey { get; set; }
        public string? SaltDerivationPattern { get; set; }
        public string? DctPermutationContext { get; set; }
        public string? BlocksContext { get; set; }
        public bool ShowHelp { get; set; }
        public bool UsingDefaultPassword { get; set; }
    }

    private static void PrintHeader()
    {
        Console.WriteLine();
        Console.WriteLine(new string('#', 60));
        Console.WriteLine("ImageScrambler");
        Console.WriteLine("Copyright (c) 2024-2025 app-pub & Churyne. All Rights Reserved.");
        Console.WriteLine("Repository: https://github.com/app-pub/ImageScrambler");
        Console.WriteLine(new string('#', 60));
        Console.WriteLine();
    }

    private static async Task<int> Main(string[] args)
    {
        try
        {
            // Set console encoding for better international character support
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    Console.OutputEncoding = Encoding.UTF8;
                    Console.InputEncoding = Encoding.UTF8;
                }
                catch
                {
                    // Ignore encoding setup errors on some systems
                }
            }

            PrintHeader();

            var options = ParseArguments(args);

            if (options.ShowHelp || string.IsNullOrEmpty(options.Command))
            {
                Console.WriteLine(LocalizationManager.GetMessage("help"));
                return 0;
            }

            if (!ValidateOptions(options))
            {
                return 1;
            }

            await ExecuteCommand(options);
            Console.WriteLine(LocalizationManager.GetMessage("info_success"));
            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_operation_failed", ex.Message));
            return 1;
        }
        catch (FileNotFoundException ex)
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_file_not_found", ex.FileName ?? "unknown"));
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_operation_failed", ex.Message));
            return 1;
        }
    }

    private static CommandOptions ParseArguments(string[] args)
    {
        var options = new CommandOptions();

        if (args.Length == 0)
        {
            options.ShowHelp = true;
            return options;
        }

        int argIndex = 0;

        // Parse command
        if (argIndex < args.Length)
        {
            var command = args[argIndex].ToLowerInvariant();
            if (command == "help" || command == "--help" || command == "-h")
            {
                options.ShowHelp = true;
                return options;
            }
            options.Command = command;
            argIndex++;
        }

        // Parse required arguments for encode/decode
        if (options.Command == "encode" || options.Command == "decode")
        {
            if (argIndex < args.Length)
            {
                options.InputPath = CleanPath(args[argIndex]);
                argIndex++;
            }

            if (argIndex < args.Length)
            {
                options.OutputPath = CleanPath(args[argIndex]);
                argIndex++;
            }

            // Password is now optional
            if (argIndex < args.Length && !args[argIndex].StartsWith("--") && !args[argIndex].StartsWith("-"))
            {
                options.Password = args[argIndex];
                argIndex++;
            }
            else
            {
                // Use default password if not provided
                options.Password = DefaultPassword;
                options.UsingDefaultPassword = true;
            }
        }

        // Parse optional arguments
        while (argIndex < args.Length)
        {
            var arg = args[argIndex].ToLowerInvariant();

            switch (arg)
            {
                case "--quality":
                case "-q":
                    if (argIndex + 1 < args.Length && int.TryParse(args[argIndex + 1], out int quality))
                    {
                        options.Quality = quality;
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException(LocalizationManager.GetMessage("error_invalid_quality"));
                    }
                    break;

                case "--no-dct":
                    options.UseDctScrambling = false;
                    argIndex++;
                    break;

                case "--salt-context":
                    if (argIndex + 1 < args.Length)
                    {
                        options.SaltContext = args[argIndex + 1];
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException("Missing salt context value");
                    }
                    break;

                case "--salt-key":
                    if (argIndex + 1 < args.Length)
                    {
                        options.SaltDerivationKey = args[argIndex + 1];
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException("Missing salt key value");
                    }
                    break;

                case "--salt-pattern":
                    if (argIndex + 1 < args.Length)
                    {
                        options.SaltDerivationPattern = args[argIndex + 1];
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException("Missing salt pattern value");
                    }
                    break;

                case "--dct-context":
                    if (argIndex + 1 < args.Length)
                    {
                        options.DctPermutationContext = args[argIndex + 1];
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException("Missing DCT context value");
                    }
                    break;

                case "--blocks-context":
                    if (argIndex + 1 < args.Length)
                    {
                        options.BlocksContext = args[argIndex + 1];
                        argIndex += 2;
                    }
                    else
                    {
                        throw new ArgumentException("Missing blocks context value");
                    }
                    break;

                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    return options;

                default:
                    throw new ArgumentException($"Unknown option: {args[argIndex]}");
            }
        }

        return options;
    }

    private static string CleanPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        // Remove surrounding quotes if present
        path = path.Trim();
        if ((path.StartsWith('"') && path.EndsWith('"')) ||
            (path.StartsWith('\'') && path.EndsWith('\'')))
        {
            path = path[1..^1];
        }

        return path.Trim();
    }

    private static bool ValidateOptions(CommandOptions options)
    {
        // Validate command
        if (options.Command != "encode" && options.Command != "decode")
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_invalid_command", options.Command));
            return false;
        }

        // Validate required arguments (password is now optional)
        if (string.IsNullOrWhiteSpace(options.InputPath) ||
            string.IsNullOrWhiteSpace(options.OutputPath))
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_missing_args"));
            return false;
        }

        // Validate paths are not empty
        if (string.IsNullOrWhiteSpace(options.InputPath) || string.IsNullOrWhiteSpace(options.OutputPath))
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_empty_path"));
            return false;
        }

        // Validate paths are different
        try
        {
            if (Path.GetFullPath(options.InputPath).Equals(Path.GetFullPath(options.OutputPath), StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(LocalizationManager.GetMessage("error_same_paths"));
                return false;
            }
        }
        catch
        {
            // If path validation fails, continue - let the file operations handle it
        }

        // Validate input file exists
        if (!File.Exists(options.InputPath))
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_file_not_found", options.InputPath));
            return false;
        }

        // Validate quality range
        if (options.Quality < 1 || options.Quality > 100)
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_invalid_quality"));
            return false;
        }

        // Validate salt pattern if provided
        if (!string.IsNullOrEmpty(options.SaltDerivationPattern) && !options.SaltDerivationPattern.Contains("{0}"))
        {
            Console.Error.WriteLine(LocalizationManager.GetMessage("error_invalid_pattern"));
            return false;
        }

        return true;
    }

    private static async Task ExecuteCommand(CommandOptions options)
    {
        Console.WriteLine(LocalizationManager.GetMessage("info_processing", options.InputPath, options.OutputPath));

        // Show default password warning if using default password
        if (options.UsingDefaultPassword)
        {
            Console.WriteLine(LocalizationManager.GetMessage("info_using_default_password"));
        }

        // Display current parameters
        Console.WriteLine(LocalizationManager.GetMessage("param_quality", options.Quality));
        Console.WriteLine(LocalizationManager.GetMessage("param_dct",
            options.UseDctScrambling ? LocalizationManager.GetMessage("param_enabled") : LocalizationManager.GetMessage("param_disabled")));
        Console.WriteLine(LocalizationManager.GetMessage("param_password", 
            options.UsingDefaultPassword ? DefaultPassword : "***"));

        if (!string.IsNullOrEmpty(options.SaltContext))
            Console.WriteLine(LocalizationManager.GetMessage("param_salt_context", options.SaltContext));
        if (!string.IsNullOrEmpty(options.SaltDerivationKey))
            Console.WriteLine(LocalizationManager.GetMessage("param_salt_key", options.SaltDerivationKey));
        if (!string.IsNullOrEmpty(options.SaltDerivationPattern))
            Console.WriteLine(LocalizationManager.GetMessage("param_salt_pattern", options.SaltDerivationPattern));
        if (!string.IsNullOrEmpty(options.DctPermutationContext))
            Console.WriteLine(LocalizationManager.GetMessage("param_dct_context", options.DctPermutationContext));
        if (!string.IsNullOrEmpty(options.BlocksContext))
            Console.WriteLine(LocalizationManager.GetMessage("param_blocks_context", options.BlocksContext));

        Console.WriteLine();

        // Create cipher instance
        var cipher = new ChromaShiftCipher(
            password: options.Password,
            useDctScrambling: options.UseDctScrambling,
            saltContext: options.SaltContext,
            saltDerivationKey: options.SaltDerivationKey,
            saltDerivationPattern: options.SaltDerivationPattern,
            dctPermutationContext: options.DctPermutationContext,
            blocksContext: options.BlocksContext
        );

        // Create output directory if it doesn't exist
        var outputDir = Path.GetDirectoryName(options.OutputPath);
        if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Execute command
        try
        {
            if (options.Command == "encode")
            {
                Console.WriteLine(LocalizationManager.GetMessage("info_encoding"));
                await cipher.EncodeAsync(options.InputPath, options.OutputPath, options.Quality);
            }
            else if (options.Command == "decode")
            {
                Console.WriteLine(LocalizationManager.GetMessage("info_decoding"));
                await cipher.DecodeAsync(options.InputPath, options.OutputPath, options.Quality);
            }
        }
        catch (ArgumentException ex) when (ex.Message.Contains("height is not divisible by 3"))
        {
            throw new ArgumentException(LocalizationManager.GetMessage("error_invalid_carrier"));
        }
    }
}