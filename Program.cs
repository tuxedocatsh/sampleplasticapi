using System;
using Codice.CmdRunner;
using System.IO;
using System.Runtime.InteropServices;

namespace sampleplasticapi
{
    class Program
    {
        public static void Main(string[] args)
        {
            CliArgumentParser cli = new CliArgumentParser(args);
            if (!cli.IsCorrect)
            {
                Environment.ExitCode = WRONG_USAGE_CODE;
                return;
            }

            // The API is implemented to take advantage of the `cm shell' command
            // The first executed command will open a shell which will receive PlasticSCM commands 
            // This optimizes the execution since the CLI environment is initialized only once.
            try
            {

                // Set up our work environment -> repository + workspace

                if (!PlasticAPI.CreateRepository(cli.RepositoryName, out mError))
                    AbortProcess();


                // Get repository name and guid
                Console.WriteLine("Repository name and guid");
                string info = PlasticAPI.GetRepositoryInformation(cli.RepositoryName);
                Console.WriteLine(info);
                string workspaceDir = Path.Combine(GetBasePath(cli), cli.WorkspaceName);

                if (Directory.Exists(workspaceDir))
                {
                    mError = string.Format("Targeted workspace directory already exists: {0}", workspaceDir);
                    AbortProcess();
                }

                Directory.CreateDirectory(workspaceDir);
                PlasticAPI.WorkingDirectory = workspaceDir;

                if (!PlasticAPI.CreateWorkspace(cli.WorkspaceName, cli.RepositoryName, out mError))
                    AbortProcess();

                string directoryPath = Path.Combine(workspaceDir, "foo");
                Directory.CreateDirectory(directoryPath);
                string fooPath = Path.Combine(directoryPath, "foo.c");
                File.WriteAllText(fooPath, HELLO_WORLD_C);

                string directorybarPath = Path.Combine(workspaceDir, "bar");
                Directory.CreateDirectory(directorybarPath);

                string barPath = Path.Combine(directorybarPath, "bar.c");
                File.WriteAllText(barPath, HELLO_WORLD_C);

                if (!PlasticAPI.Add(directoryPath, true, out mError))
                    AbortProcess();
                if (!PlasticAPI.Add(directorybarPath, true, out mError))
                    AbortProcess();

                CheckIsItemCheckedOut(directoryPath);
                CheckIsItemCheckedOut(directorybarPath);
                CheckIsItemCheckedOut(fooPath);
                CheckIsItemCheckedOut(barPath);

                if (!PlasticAPI.Checkin(
                    "Check in all remaining contents -> a recursively added directory tree.", out mError))
                {
                    AbortProcess();
                }

                if (!PlasticAPI.Update(out mError))
                    AbortProcess();

                Console.WriteLine("Changeset " + PlasticAPI.GetChangeset());
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format("ERROR: {0}", e.Message));
                Environment.ExitCode = GENERIC_ERROR_CODE;
            }
            finally
            {
                CmdRunner.TerminateShell();
            }
        }
        private static void AbortProcess()
        {
            throw new Exception(mError);
        }

        private static string GetBasePath(CliArgumentParser cli)
        {
            if (cli.HasOption(CliOptions.LocalArgument))
                return Environment.CurrentDirectory;
            return Path.GetTempPath();
        }

        private static void CheckIsItemCheckedOut(string path)
        {
            if (PlasticAPI.IsCheckedOut(path))
                return;

            mError = string.Format("File '{0}' should be checked out, but it isn't.", path);
            AbortProcess();
        }
        private static string mError = string.Empty;

        const int GENERIC_ERROR_CODE = 1;
        const int WRONG_USAGE_CODE = 2;

        const string HELLO_WORLD_C =
@"#include <stdio.h>

int main(int argc, char **argv) {
    printf(""Hello, world!\n"");
    return 0;
}
";
    }
}
