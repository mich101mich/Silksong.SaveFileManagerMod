"""
Script to check translatable string declarations against the existing language files.
"""
import json
import os
import re
import sys

class LanguageEntry:
    """
    Represents a language entry
    """
    def __init__(self, line: str, class_name: str):
        self.class_name = class_name

        # line format property: '<Name> => Get("<key>", "<fallback>");'
        # line format method: '<Name>(<args...>) => Get("<key>", "<fallback>", new { <args...> });'
        parts = line.split(" => Get(")

        self.name = parts[0]
        open_paren_index = self.name.find("(")
        if open_paren_index != -1:
            self.name = self.name[:open_paren_index]

        call_args = parts[1]
        if not call_args.endswith(");"):
            raise ValueError(f"Invalid line format (missing closing parenthesis): {line}")
        call_args = call_args[:-2]  # Remove trailing ');'

        if not call_args.startswith('"'):
            raise ValueError(f"Invalid line format (missing opening quote for key): {line}")

        key_start = 1  # Skip the opening quote
        key_end = call_args.find('", "', key_start)
        self.key = call_args[key_start:key_end]

        fallback_start = key_end + len('", "')

        dynamic_args_start = call_args.find('", new {', key_end)
        if dynamic_args_start == -1:
            # No dynamic arguments, fallback is the second argument

            if not call_args.endswith('"'):
                raise ValueError(f"Invalid line format (missing closing quote for fallback): {line}")

            self.fallback = call_args[fallback_start:-1]  # Exclude the closing quote
            self.dynamic_args = set()
        else:
            # Dynamic arguments present, fallback is the second argument
            fallback_end = dynamic_args_start
            self.fallback = call_args[fallback_start:fallback_end]

            if not call_args.endswith("}"):
                raise ValueError(f"Invalid line format (missing closing brace for args): {line}")

            dynamic_args_start = dynamic_args_start + len('", new {')
            dynamic_args_end = len(call_args) - 1  # Exclude the closing brace
            dynamic_args = call_args[dynamic_args_start:dynamic_args_end].split(",")
            self.dynamic_args = set(arg.strip() for arg in dynamic_args)

        # Fallback might contain escaped quotes, unescape them
        self.fallback = self.fallback.replace('\\"', '"')

    def entry_name(self):
        """
        Returns the entry name
        """
        return f"{self.class_name}.{self.name}"


# Structure:
# public partial class <ClassName>
# {
#     public class Strings : StringHelper
#     {
#         public static LT <Name> => Get("<key>", "<fallback>");
#         public static LT <Name>(<args...>) => Get("<key>", "<fallback>", new { <args...> });
#     }
# }
CLASS_DECLARATION_PREFIX = "public partial class "
MEMBER_DECLARATION_PREFIX = "        public static LT "

def main():
    """
    Main function
    """
    current_file_dir = os.path.dirname(os.path.abspath(__file__))
    language_files_dir = os.path.join(current_file_dir, "..", "languages")
    code_declaration_file = os.path.join(current_file_dir, "..", "src", "UI", "Strings.cs")

    found_entries = {}
    has_errors = False

    with open(code_declaration_file, "r", encoding="utf-8") as f:
        current_class = None
        for line in f:
            line = line.strip("\n\r")
            if line.startswith(CLASS_DECLARATION_PREFIX):
                current_class = line[len(CLASS_DECLARATION_PREFIX):]
                print(f"Current class: '{current_class}'")
            elif line == "}":
                current_class = None
            elif current_class is not None and line.startswith(MEMBER_DECLARATION_PREFIX):
                entry = LanguageEntry(line[len(MEMBER_DECLARATION_PREFIX):], current_class)
                print(f"Found entry: '{entry.entry_name()}': key='{entry.key}', fallback='{entry.fallback}', dynamic_args={entry.dynamic_args}")
                found_entries[entry.entry_name()] = entry

                if entry.key != entry.entry_name():
                    print(f"Error: Key '{entry.key}' does not match entry name '{entry.entry_name()}'")
                    has_errors = True

    for lang_file_name in os.listdir(language_files_dir):
        if not lang_file_name.endswith(".json"):
            continue
        lang_name = lang_file_name[:-5]  # Remove .json extension

        lang_file_path = os.path.join(language_files_dir, lang_file_name)
        with open(lang_file_path, "r", encoding="utf-8") as f:
            lang_data = json.load(f)

        for entry_name, entry in found_entries.items():
            if entry.key not in lang_data:
                print(f"Error: Key '{entry.key}' for entry '{entry_name}' not found in language file '{lang_file_name}'")
                has_errors = True
                continue

            lang_value = lang_data[entry.key]

            # Find any placeholders in the fallback value (e.g., {index}, {name})
            lang_placeholders = set(re.findall(r"\{(\w+)\}", lang_value))

            for placeholder in entry.dynamic_args:
                if placeholder not in lang_placeholders:
                    print(f"Error: Placeholder '{{{placeholder}}}' for entry '{entry_name}' not found in language file '{lang_file_name}'")
                    has_errors = True
            for placeholder in lang_placeholders:
                if placeholder not in entry.dynamic_args:
                    print(f"Error: Placeholder '{{{placeholder}}}' in language file '{lang_file_name}' for entry '{entry_name}' is not declared in the code")
                    has_errors = True

            # Check if the fallback value matches
            if lang_name == "en" and lang_value != entry.fallback:
                print(f"""Error: Fallback value for key '{entry.key}' in language file '{lang_file_name}' does not match the code declaration.
Expected: '{entry.fallback}', Found: '{lang_value}'""")
                has_errors = True

        for key in lang_data.keys():
            if key not in [entry.key for entry in found_entries.values()]:
                print(f"Error: Key '{key}' in language file '{lang_file_name}' does not have a corresponding code declaration.")
                has_errors = True

    if has_errors:
        print("Errors found during language file check.")
        sys.exit(1)
    else:
        print("All language entries are valid and present in the language files.")

if __name__ == "__main__":
    main()
