using System.CommandLine;
using System.Threading.Tasks;

namespace fib
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand("root command for file bundle CLI");
            var bundleCommand = new Command("bundle", "Bundle code files to a single file");

            var languageOption = new Option<string[]>("--language", "-l")
            {
                Description = "selected language for the files",
                Required = true,
                Arity = ArgumentArity.OneOrMore
            };
            var outputOption = new Option<string>("--output", "-o")
            {
                Description = "the destination of the combine files",
                Required = true,
            };
            var noteOption = new Option<bool>("--note", "-n")
            {
                Description = "add source file information as a comment",
            };
            var sortOption = new Option<string>("--sort", "-s")
            {
                Description = "sort file by name or by type ",
                DefaultValueFactory = _ => "name"
            };
            var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "-r")
            {
                Description = "remove empty line from sorce files",

            };
            var authorOption = new Option<string>("--author", "-a")
            {
                Description = "Author name"

            };

            var createRspCommand = new Command("create-rsp", "creat rsp response file to the command");


            bundleCommand.SetAction((parseResult) =>
            {
                string[] languages = parseResult.GetValue(languageOption) ?? Array.Empty<string>();
                string output = parseResult.GetValue(outputOption) ?? string.Empty;
                string sortby = parseResult.GetValue(sortOption) ?? "name";
                bool note = parseResult.GetValue(noteOption);
                bool removeEmptyLine = parseResult.GetValue(removeEmptyLinesOption);
                string author = parseResult.GetValue(authorOption) ?? string.Empty;

                string currentDirectory = Directory.GetCurrentDirectory();

                try
                {
                    string[] files = GetCodeFiles(currentDirectory, languages);
                    string outputPath = Path.GetFullPath(output);

                    if (files.Any(file =>
                        string.Equals(
                            Path.GetFullPath(file),
                            outputPath,
                            StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new ArgumentException(
                            "Output file cannot be one of the source files.");
                    }

                    files = SortFiles(files, sortby);
                    string combinedContet = CombineFiles(files, note, removeEmptyLine, author);
                    File.WriteAllText(output, combinedContet);

                }
                catch (IOException ex)
                {

                    Console.WriteLine($"File ERROR: {ex.Message} ");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Permission ERROR: {ex.Message}");
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine($"Invalid input ERROR:{e.Message}");
                }
            });
            createRspCommand.SetAction((parseResult) =>
            {
                Console.Write("Enter languages (for example: cs js, or all): ");
                string languages = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(languages))
                {
                    Console.WriteLine("Invalid input. Languages cannot be empty.");
                    return;
                }

                Console.Write("Enter output file name: ");
                string output = Console.ReadLine() ?? "";

                if (string.IsNullOrWhiteSpace(output))
                {
                    Console.WriteLine("Invalid input. Output file name cannot be empty.");
                    return;
                }

                Console.Write("Include file notes? (y/n): ");
                string note = Console.ReadLine() ?? "n";
                note = note.Trim();
                if (!note.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                  !note.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                    return;
                }

                Console.Write("Sort by name or type: ");
                string sort = Console.ReadLine() ?? "name";


                sort = sort.Trim();
                if (!sort.Equals("name", StringComparison.OrdinalIgnoreCase) &&
                    !sort.Equals("type", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid sort. Please enter 'name' or 'type'.");
                    return;
                }


                Console.Write("Remove empty lines? (y/n): ");
                string removeEmptyLines = Console.ReadLine() ?? "n";
                removeEmptyLines = removeEmptyLines.Trim();
                if (!removeEmptyLines.Equals("y", StringComparison.OrdinalIgnoreCase) &&
                   !removeEmptyLines.Equals("n", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Invalid input. Please enter 'y' or 'n'.");
                    return;
                }


                Console.Write("Enter author name (optional): ");
                string author = Console.ReadLine() ?? "";

                Console.Write("Enter response file name: ");
                string responseFile = Console.ReadLine() ?? "bundle.rsp";
                //----------------------------------------------------------

                string command = $"bundle --language {languages} --output \"{output}\"";

                if (note.Equals("y", StringComparison.OrdinalIgnoreCase))
                    command += " --note";

                if (removeEmptyLines.Equals("y", StringComparison.OrdinalIgnoreCase))
                    command += " --remove-empty-lines";

                if (!string.IsNullOrWhiteSpace(author))
                    command += $" --author \"{author}\"";

                command += $" --sort {sort}";

                try
                {
                    File.WriteAllText(responseFile, command);
                    Console.WriteLine($"Response file created successfully: {responseFile}");
                }
                catch (IOException e)
                {
                    Console.WriteLine($"File error: {e.Message}");
                }
                catch (UnauthorizedAccessException e)
                {
                    Console.WriteLine($"Permission error: {e.Message}");
                }
            });

            rootCommand.Subcommands.Add(bundleCommand);
            rootCommand.Subcommands.Add(createRspCommand);

            bundleCommand.Options.Add(languageOption);
            bundleCommand.Options.Add(outputOption);
            bundleCommand.Options.Add(noteOption);
            bundleCommand.Options.Add(sortOption);
            bundleCommand.Options.Add(removeEmptyLinesOption);
            bundleCommand.Options.Add(authorOption);

            var parseResult = rootCommand.Parse(args);

            if (parseResult.Errors.Count > 0)
            {
                Console.WriteLine("Invalid command input.");

                foreach (var error in parseResult.Errors)
                {
                    Console.WriteLine($"Error: {error.Message}");
                }

                return 1;
            }


            return await parseResult.InvokeAsync();
        }

        static string[] GetCodeFiles(string directory, string[] languages)
        {
            string[] codeExtensions = { ".cs", ".js", ".ts", ".jsx", ".tsx", ".py", ".java", ".cpp", ".c", ".h", ".html", ".css" };
            string[] selectedExtensions;




            if (languages.Any(l => l.Equals("all", StringComparison.OrdinalIgnoreCase)))
            {
                selectedExtensions = codeExtensions;
            }
            else
            {
                selectedExtensions = languages
               .Select(language => "." + language.TrimStart('.').ToLower())
               .ToArray();
            }

            var invalidLanguages = selectedExtensions
                .Except(codeExtensions, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (invalidLanguages.Length > 0)
            {
                throw new ArgumentException(
                    $"Unsupported language(s): {string.Join(", ", invalidLanguages)}");
            }


            return GetFilesExceptBuildFolders(directory)
                .Where(file => codeExtensions.Contains(Path.GetExtension(file).ToLower()))
                .Where(file => selectedExtensions.Contains(Path.GetExtension(file).ToLower()))
                .ToArray();
        }

        static string[] SortFiles(string[] files, string sortBy)
        {
            if (sortBy == "name")
            {
                return files
                    .OrderBy(file => Path.GetFileName(file))
                    .ToArray();
            }

            if (sortBy == "type")
            {
                return files
                    .OrderBy(file => Path.GetExtension(file))
                    .ThenBy(file => Path.GetFileName(file))
                    .ToArray();
            }

            throw new ArgumentException("Sort must be 'name' or 'type'.");
        }

        static string CombineFiles(string[] files, bool note, bool removeEmptyLines, string author)
        {
            string res = "";
            string content = "";

            if (!string.IsNullOrEmpty(author))
                res += $"//Author: {author}{Environment.NewLine}";


            foreach (string file in files)
            {
                if (note)
                    res += $"// File: {Path.GetRelativePath(Directory.GetCurrentDirectory(), file)}{Environment.NewLine}";

                content = File.ReadAllText(file);
                if (removeEmptyLines)
                {
                    content = string.Join(
                        Environment.NewLine,
                        content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                               .Where(line => !string.IsNullOrWhiteSpace(line))
                    );

                }
                res += content + Environment.NewLine;
            }


            return res;


        }

        static IEnumerable<string> GetFilesExceptBuildFolders(string directory)
        {
            string[] excludedFolders =
            {
                "bin",
                "obj",
                "debug",
                "release",
                ".git",
                "node_modules"
              };

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                yield return file;
            }

            foreach (string subdirectory in Directory.EnumerateDirectories(directory))
            {
                string folderName = Path.GetFileName(subdirectory);

                if (excludedFolders.Contains(
                    folderName,
                    StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string file in GetFilesExceptBuildFolders(subdirectory))
                {
                    yield return file;
                }
            }
        }
    }

}
