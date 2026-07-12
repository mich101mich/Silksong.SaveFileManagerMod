using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using SaveFileManagerMod.UI;

namespace SaveFileManagerMod.Tools.LocalizationChecker;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: LocalizationChecker <path_to_SaveFileManagerMod_assembly> <path_to_languages_directory>");
            return 1;
        }

        var assemblyPath = args[0];
        var languagesDir = args[1];

        Console.WriteLine($"Assembly Path: {assemblyPath}");
        Console.WriteLine($"Languages Directory: {languagesDir}");

        var hasErrors = false;

        var assembly = Assembly.LoadFrom(assemblyPath);
        var stringHelperType = assembly.GetType(typeof(StringHelper).FullName!)
            ?? throw new Exception("Could not find StringHelper type in assembly.");

        // Check for the LocalizationKey field first to ensure that TESTING code is active
        var keyInfoField = stringHelperType.GetField("LocalizationKey", BindingFlags.Public | BindingFlags.Static)
            ?? throw new Exception("Could not find LocalizationKey field in StringHelper type. Is the TESTING symbol defined?");
        keyInfoField.SetValue(null, null); // Reset the LocalizationKey to null before starting

        var keyInfoType = stringHelperType.GetNestedType("KeyInfo") ?? throw new Exception("Could not find KeyInfo nested type in StringHelper type.");
        var keyField = keyInfoType.GetField("Key") ?? throw new Exception("Could not find Key field in KeyInfo type.");
        var fallbackField = keyInfoType.GetField("Fallback") ?? throw new Exception("Could not find Fallback field in KeyInfo type.");
        var placeholdersField = keyInfoType.GetField("Placeholders") ?? throw new Exception("Could not find Placeholders field in KeyInfo type.");

        var foundKeys = new Dictionary<string, (string fallback, List<string> placeholders)>();

        // Force static constructors to run for all types that might contain localization keys
        var localizableTypes = assembly.GetTypes()
            .Where(t => t.IsAssignableTo(stringHelperType))
            .Where(t => t != stringHelperType); // Exclude StringHelper itself

        foreach (var type in localizableTypes)
        {
            // type has the form SaveFileManagerMod.UI.{classname}+Strings
            var classname = type.FullName?.Split('.').Last()?.Split('+')[0]
                ?? throw new Exception($"Could not determine class name for type {type.FullName}");

            Console.WriteLine($"Processing strings from {classname}");

            var members = new List<(string name, Action invoke)>();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Static))
            {
                Console.WriteLine($"Found property: {classname}.{prop.Name}");
                members.Add((prop.Name, () => prop.GetValue(null)));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.IsSpecialName)
                {
                    continue; // Skip property getters/setters
                }

                var parameters = method.GetParameters();
                Console.WriteLine($"Found method: {classname}.{method.Name} ( {string.Join(", ", parameters.Select(p => p.ParameterType.Name))} )");
                members.Add((method.Name, () => method.Invoke(null, parameters.Select(p => (object)null).ToArray())));
            }

            foreach (var (name, invoke) in members)
            {
                var expectedKey = $"{classname}.{name}";

                invoke.Invoke();

                var currentKeyInfo = keyInfoField.GetValue(null);
                keyInfoField.SetValue(null, null); // Reset the LocalizationKey to null for the next property
                if (currentKeyInfo == null)
                {
                    Console.Error.WriteLine($"Member {name} did not set LocalizationKey. Ensure that the Get method is called with the correct parameters.");
                    hasErrors = true;
                    continue;
                }

                var key = keyField.GetValue(currentKeyInfo) as string ?? throw new Exception("Key is null or not a string.");
                var fallback = fallbackField.GetValue(currentKeyInfo) as string ?? throw new Exception("Fallback is null or not a string.");
                var placeholders = placeholdersField.GetValue(currentKeyInfo) as List<string> ?? throw new Exception("Placeholders is null or not a list.");

                if (key != expectedKey)
                {
                    Console.Error.WriteLine($"Member {name} set an unexpected key: {key}. Expected: {expectedKey}");
                    hasErrors = true;
                }

                foundKeys[expectedKey] = (fallback, placeholders);
            }
        }

        Console.WriteLine($"Found {foundKeys.Count} localization keys in the assembly.");

        var languageFiles = Directory.GetFiles(languagesDir, "*.json");

        foreach (var file in languageFiles)
        {
            Console.WriteLine($"\n--- Checking {Path.GetFileName(file)} ---");
            var json = File.ReadAllText(file);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

            foreach (var (key, (fallback, placeholders)) in foundKeys)
            {
                // 1. Check if key exists
                if (!translations.ContainsKey(key))
                {
                    Console.Error.WriteLine($"Missing key: {key}");
                    hasErrors = true;
                    continue;
                }

                var translation = translations[key];

                // 2. Check placeholders
                var translationPlaceholders = Regex.Matches(translation, @"\{(\w+)\}")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet();

                var expectedPlaceholders = placeholders.ToHashSet();

                foreach (var placeholder in expectedPlaceholders)
                {
                    if (!translationPlaceholders.Contains(placeholder))
                    {
                        Console.Error.WriteLine($"Missing placeholder in translation for key '{key}': {{{placeholder}}}");
                        hasErrors = true;
                    }
                }
                foreach (var placeholder in translationPlaceholders)
                {
                    if (!expectedPlaceholders.Contains(placeholder))
                    {
                        Console.Error.WriteLine($"Unexpected placeholder in translation for key '{key}': {{{placeholder}}}");
                        hasErrors = true;
                    }
                }

                // 3. Check fallback string for english translation
                if (Path.GetFileName(file) == "en.json")
                {
                    if (fallback != translation)
                    {
                        Console.Error.WriteLine($"Fallback mismatch for key '{key}'. Expected: '{fallback}', Found: '{translation}'");
                        hasErrors = true;
                    }
                }
            }

            // Check for extra keys in json
            foreach (var key in translations.Keys)
            {
                if (!foundKeys.ContainsKey(key))
                {
                    Console.Error.WriteLine($"Extra key in {Path.GetFileName(file)}: {key}");
                    hasErrors = true;
                }
            }
        }

        if (hasErrors)
        {
            Console.Error.WriteLine("\nLocalization check failed with errors.");
            return 1;
        }

        Console.WriteLine("\nLocalization check passed successfully.");
        return 0;
    }
}
