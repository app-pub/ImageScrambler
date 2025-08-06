using System.Collections.Frozen;
using System.Globalization;

namespace ImageScrambler;

public static class LocalizationManager
{
    private static readonly FrozenDictionary<string, FrozenDictionary<string, string>> LocalizedMessages = 
        CreateLocalizedMessages();

    private static readonly string CurrentLanguage = DetermineLanguage();

    private static FrozenDictionary<string, FrozenDictionary<string, string>> CreateLocalizedMessages()
    {
        var messages = new Dictionary<string, FrozenDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        // English (Default/Fallback)
        var englishMessages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = "ImageScrambler - Advanced Image Encryption Tool\n\n" +
                      "USAGE:\n" +
                      "  ImageScrambler encode <input> <output> [password] [options]\n" +
                      "  ImageScrambler decode <input> <output> [password] [options]\n" +
                      "  ImageScrambler help\n\n" +
                      "COMMANDS:\n" +
                      "  encode                 Encode/hide an image using ChromaShift cipher\n" +
                      "  decode                 Decode/extract hidden image from carrier\n" +
                      "  help                   Show this help information\n\n" +
                      "ARGUMENTS:\n" +
                      "  <input>                Input image file path\n" +
                      "  <output>               Output image file path\n" +
                      "  [password]             Password for encryption/decryption (optional, default: \"Default@Password\")\n\n" +
                      "OPTIONS:\n" +
                      "  --quality, -q          JPEG quality (1-100, default: 100)\n" +
                      "  --no-dct               Disable DCT scrambling (default: enabled)\n" +
                      "  --salt-context         Custom salt context\n" +
                      "  --salt-key             Custom salt derivation key\n" +
                      "  --salt-pattern         Custom salt derivation pattern (must contain {0})\n" +
                      "  --dct-context          Custom DCT permutation context\n" +
                      "  --blocks-context       Custom blocks context\n" +
                      "  --help, -h             Show this help information\n\n" +
                      "EXAMPLES:\n" +
                      "  ImageScrambler encode input.jpg output.jpg\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword\n" +
                      "  ImageScrambler encode \"C:\\path with spaces\\input.png\" output.jpg mypassword --quality 95\n" +
                      "  ImageScrambler decode carrier.jpg hidden.jpg mypassword --no-dct\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword --salt-context \"custom\" --quality 80\n",
            
            ["error_invalid_args"] = "Error: Invalid arguments. Use 'ImageScrambler help' for usage information.",
            ["error_file_not_found"] = "Error: Input file '{0}' not found.",
            ["error_invalid_quality"] = "Error: Quality must be a number between 1 and 100.",
            ["error_invalid_pattern"] = "Error: Salt pattern must contain '{{0}}' placeholder.",
            ["error_empty_password"] = "Error: Password cannot be empty.",
            ["error_empty_path"] = "Error: File paths cannot be empty.",
            ["error_same_paths"] = "Error: Input and output paths cannot be the same.",
            ["error_invalid_command"] = "Error: Unknown command '{0}'. Valid commands: encode, decode, help",
            ["error_missing_args"] = "Error: Missing required arguments. Expected: <command> <input> <output> [password]",
            ["error_operation_failed"] = "Error: Operation failed - {0}",
            ["error_invalid_carrier"] = "Error: Invalid carrier image - height is not divisible by 3.",
            
            ["info_encoding"] = "Encoding image...",
            ["info_decoding"] = "Decoding image...", 
            ["info_success"] = "Operation completed successfully.",
            ["info_processing"] = "Processing: {0} -> {1}",
            ["info_using_default_password"] = "No password provided, using default password: \"Default@Password\"",
            
            ["param_quality"] = "JPEG Quality: {0}",
            ["param_dct"] = "DCT Scrambling: {0}",
            ["param_password"] = "Password: {0}",
            ["param_salt_context"] = "Salt Context: {0}",
            ["param_salt_key"] = "Salt Key: {0}",
            ["param_salt_pattern"] = "Salt Pattern: {0}",
            ["param_dct_context"] = "DCT Context: {0}",
            ["param_blocks_context"] = "Blocks Context: {0}",
            ["param_enabled"] = "Enabled",
            ["param_disabled"] = "Disabled"
        };

        // Simplified Chinese
        var zhCnMessages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = "ImageScrambler - 高级图像加密工具\n\n" +
                      "用法:\n" +
                      "  ImageScrambler encode <输入> <输出> [密码] [选项]\n" +
                      "  ImageScrambler decode <输入> <输出> [密码] [选项]\n" +
                      "  ImageScrambler help\n\n" +
                      "命令:\n" +
                      "  encode                 使用色度偏移密码编码/隐藏图像\n" +
                      "  decode                 从载体中解码/提取隐藏图像\n" +
                      "  help                   显示此帮助信息\n\n" +
                      "参数:\n" +
                      "  <输入>                 输入图像文件路径\n" +
                      "  <输出>                 输出图像文件路径\n" +
                      "  [密码]                 用于加密/解密的密码（可选，默认：\"Default@Password\"）\n\n" +
                      "选项:\n" +
                      "  --quality, -q          JPEG质量 (1-100, 默认: 100)\n" +
                      "  --no-dct               禁用DCT扰码 (默认: 启用)\n" +
                      "  --salt-context         自定义盐值上下文\n" +
                      "  --salt-key             自定义盐值派生密钥\n" +
                      "  --salt-pattern         自定义盐值派生模式 (必须包含 {0})\n" +
                      "  --dct-context          自定义DCT排列上下文\n" +
                      "  --blocks-context       自定义块上下文\n" +
                      "  --help, -h             显示此帮助信息\n\n" +
                      "示例:\n" +
                      "  ImageScrambler encode input.jpg output.jpg\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword\n" +
                      "  ImageScrambler encode \"C:\\包含空格的路径\\input.png\" output.jpg mypassword --quality 95\n" +
                      "  ImageScrambler decode carrier.jpg hidden.jpg mypassword --no-dct\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword --salt-context \"自定义\" --quality 80\n",
            
            ["error_invalid_args"] = "错误: 无效参数。使用 'ImageScrambler help' 查看用法信息。",
            ["error_file_not_found"] = "错误: 找不到输入文件 '{0}'。",
            ["error_invalid_quality"] = "错误: 质量必须是1到100之间的数字。",
            ["error_invalid_pattern"] = "错误: 盐值模式必须包含 '{{0}}' 占位符。",
            ["error_empty_password"] = "错误: 密码不能为空。",
            ["error_empty_path"] = "错误: 文件路径不能为空。",
            ["error_same_paths"] = "错误: 输入和输出路径不能相同。",
            ["error_invalid_command"] = "错误: 未知命令 '{0}'。有效命令: encode, decode, help",
            ["error_missing_args"] = "错误: 缺少必需参数。期望: <命令> <输入> <输出> [密码]",
            ["error_operation_failed"] = "错误: 操作失败 - {0}",
            ["error_invalid_carrier"] = "错误: 无效的载体图像 - 高度不能被3整除。",
            
            ["info_encoding"] = "正在编码图像...",
            ["info_decoding"] = "正在解码图像...", 
            ["info_success"] = "操作成功完成。",
            ["info_processing"] = "处理中: {0} -> {1}",
            ["info_using_default_password"] = "未提供密码，将使用默认密码：\"Default@Password\"",
            
            ["param_quality"] = "JPEG质量: {0}",
            ["param_dct"] = "DCT扰码: {0}",
            ["param_password"] = "密码: {0}",
            ["param_salt_context"] = "盐值上下文: {0}",
            ["param_salt_key"] = "盐值密钥: {0}",
            ["param_salt_pattern"] = "盐值模式: {0}",
            ["param_dct_context"] = "DCT上下文: {0}",
            ["param_blocks_context"] = "块上下文: {0}",
            ["param_enabled"] = "启用",
            ["param_disabled"] = "禁用"
        };

        // Traditional Chinese
        var zhTwMessages = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = "ImageScrambler - 進階圖像加密工具\n\n" +
                      "用法:\n" +
                      "  ImageScrambler encode <輸入> <輸出> [密碼] [選項]\n" +
                      "  ImageScrambler decode <輸入> <輸出> [密碼] [選項]\n" +
                      "  ImageScrambler help\n\n" +
                      "命令:\n" +
                      "  encode                 使用色度位移密碼編碼/隱藏圖像\n" +
                      "  decode                 從載體中解碼/提取隱藏圖像\n" +
                      "  help                   顯示此說明資訊\n\n" +
                      "參數:\n" +
                      "  <輸入>                 輸入圖像檔案路徑\n" +
                      "  <輸出>                 輸出圖像檔案路徑\n" +
                      "  [密碼]                 用於加密/解密的密碼（可選，預設：\"Default@Password\"）\n\n" +
                      "選項:\n" +
                      "  --quality, -q          JPEG品質 (1-100, 預設: 100)\n" +
                      "  --no-dct               停用DCT擾碼 (預設: 啟用)\n" +
                      "  --salt-context         自訂鹽值內容\n" +
                      "  --salt-key             自訂鹽值衍生金鑰\n" +
                      "  --salt-pattern         自訂鹽值衍生模式 (必須包含 {0})\n" +
                      "  --dct-context          自訂DCT排列內容\n" +
                      "  --blocks-context       自訂區塊內容\n" +
                      "  --help, -h             顯示此說明資訊\n\n" +
                      "範例:\n" +
                      "  ImageScrambler encode input.jpg output.jpg\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword\n" +
                      "  ImageScrambler encode \"C:\\包含空格的路徑\\input.png\" output.jpg mypassword --quality 95\n" +
                      "  ImageScrambler decode carrier.jpg hidden.jpg mypassword --no-dct\n" +
                      "  ImageScrambler encode input.jpg output.jpg mypassword --salt-context \"自訂\" --quality 80\n",
            
            ["error_invalid_args"] = "錯誤: 無效參數。使用 'ImageScrambler help' 檢視用法資訊。",
            ["error_file_not_found"] = "錯誤: 找不到輸入檔案 '{0}'。",
            ["error_invalid_quality"] = "錯誤: 品質必須是1到100之間的數字。",
            ["error_invalid_pattern"] = "錯誤: 鹽值模式必須包含 '{{0}}' 佔位符。",
            ["error_empty_password"] = "錯誤: 密碼不能為空。",
            ["error_empty_path"] = "錯誤: 檔案路徑不能為空。",
            ["error_same_paths"] = "錯誤: 輸入和輸出路徑不能相同。",
            ["error_invalid_command"] = "錯誤: 未知命令 '{0}'。有效命令: encode, decode, help",
            ["error_missing_args"] = "錯誤: 缺少必需參數。期望: <命令> <輸入> <輸出> [密碼]",
            ["error_operation_failed"] = "錯誤: 操作失敗 - {0}",
            ["error_invalid_carrier"] = "錯誤: 無效的載體圖像 - 高度不能被3整除。",
            
            ["info_encoding"] = "正在編碼圖像...",
            ["info_decoding"] = "正在解碼圖像...", 
            ["info_success"] = "操作成功完成。",
            ["info_processing"] = "處理中: {0} -> {1}",
            ["info_using_default_password"] = "未提供密碼，將使用預設密碼：\"Default@Password\"",
            
            ["param_quality"] = "JPEG品質: {0}",
            ["param_dct"] = "DCT擾碼: {0}",
            ["param_password"] = "密碼: {0}",
            ["param_salt_context"] = "鹽值內容: {0}",
            ["param_salt_key"] = "鹽值金鑰: {0}",
            ["param_salt_pattern"] = "鹽值模式: {0}",
            ["param_dct_context"] = "DCT內容: {0}",
            ["param_blocks_context"] = "區塊內容: {0}",
            ["param_enabled"] = "啟用",
            ["param_disabled"] = "停用"
        };

        // Add all message dictionaries
        messages["en"] = englishMessages.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        messages["zh-CN"] = zhCnMessages.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        messages["zh-TW"] = zhTwMessages.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        
        // Add aliases
        messages["zh-Hans"] = messages["zh-CN"];
        messages["zh-Hant"] = messages["zh-TW"];
        messages["zh"] = messages["zh-CN"]; // Default Chinese to Simplified

        return messages.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static string DetermineLanguage()
    {
        try
        {
            // Try to get current culture
            var culture = CultureInfo.CurrentUICulture;
            
            // First try exact match
            var exactMatch = culture.Name.ToLowerInvariant();
            if (LocalizedMessages.ContainsKey(exactMatch))
                return exactMatch;

            // Try language code only (e.g., "zh" from "zh-CN")
            var languageOnly = culture.TwoLetterISOLanguageName.ToLowerInvariant();
            if (LocalizedMessages.ContainsKey(languageOnly))
                return languageOnly;

            // For Chinese, try to be more specific
            if (languageOnly == "zh")
            {
                // Check if it's traditional or simplified based on region
                var region = culture.Name.ToLowerInvariant();
                if (region.Contains("tw") || region.Contains("hk") || region.Contains("mo") || 
                    region.Contains("hant") || culture.Name.Contains("Traditional"))
                {
                    return "zh-tw";
                }
                else
                {
                    return "zh-cn"; // Default to simplified
                }
            }

            // Fallback to English
            return "en";
        }
        catch
        {
            // If anything goes wrong, default to English
            return "en";
        }
    }

    public static string GetMessage(string key, params object[] args)
    {
        // Try current language first
        if (LocalizedMessages.TryGetValue(CurrentLanguage, out var currentMessages) &&
            currentMessages.TryGetValue(key, out var message))
        {
            return args.Length > 0 ? string.Format(CultureInfo.CurrentCulture, message, args) : message;
        }

        // Fallback to English
        if (LocalizedMessages.TryGetValue("en", out var englishMessages) &&
            englishMessages.TryGetValue(key, out var englishMessage))
        {
            return args.Length > 0 ? string.Format(CultureInfo.InvariantCulture, englishMessage, args) : englishMessage;
        }

        // If all else fails, return the key
        return key;
    }

    public static string GetCurrentLanguage() => CurrentLanguage;

    public static bool IsChineseLanguage() => CurrentLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

    public static bool IsSimplifiedChinese() => CurrentLanguage.Equals("zh-CN", StringComparison.OrdinalIgnoreCase) || 
                                                CurrentLanguage.Equals("zh-Hans", StringComparison.OrdinalIgnoreCase) ||
                                                CurrentLanguage.Equals("zh", StringComparison.OrdinalIgnoreCase);

    public static bool IsTraditionalChinese() => CurrentLanguage.Equals("zh-TW", StringComparison.OrdinalIgnoreCase) || 
                                                 CurrentLanguage.Equals("zh-Hant", StringComparison.OrdinalIgnoreCase);
}