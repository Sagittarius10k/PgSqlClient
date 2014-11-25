using System;
using System.Collections.Generic;
using System.IO;
using Npgsql;

namespace PgSqlClient
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check info request
            if (args.Length == 0)
            {
                ShowInfo();
                return;
            }

            // Environments
            var host = "localhost";
            var database = "postgres";
            var port = "5432";
            var user = "postgres";
            var password = String.Empty;
            var files = new List<string>();
            var isSilentMode = false;
            var isMultiLineMode = false;

            // Arguments prasing
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h")
                {
                    if (++i == args.Length)
                    {
                        ShowError("Error 1: The host is not specified for '-h' option.");
                        return;
                    }
                    host = args[i];
                    continue;
                }
                if (args[i] == "-d")
                {
                    if (++i == args.Length)
                    {
                        ShowError("Error 2: The database name is not specified for '-d' option.");
                        return;
                    }
                    database = args[i];
                    continue;
                }
                if (args[i] == "-p")
                {
                    if (++i == args.Length)
                    {
                        ShowError("Error 3: The server port is not specified for '-p' option.");
                        return;
                    }
                    port = args[i];
                    continue;
                }
                if (args[i] == "-U")
                {
                    if (++i == args.Length)
                    {
                        ShowError("Error 4: The user name is not specified for '-u' option.");
                        return;
                    }
                    user = args[i];
                    continue;
                }
                if (args[i] == "-P")
                {
                    if (++i == args.Length)
                    {
                        ShowError("Error 5: The user password is not specified for '-P' option.");
                        return;
                    }
                    password = args[i];
                    continue;
                }
                if (args[i] == "-s")
                {
                    isSilentMode = true;
                    continue;
                }
                if (args[i] == "-m")
                {
                    isMultiLineMode = true;
                    continue;
                }
                if (File.Exists(args[i]))
                {
                    files.Add(args[i]);
                    continue;
                }
                Console.WriteLine("Error 6: File <" + args[i] + "> does not exist.\n");

                ShowInfo();
                return;
            }

            // Executes the scripts
            try
            {
                string connectionString =
                    "Server=" + host +
                    ";Database=" + database +
                    ";Port=" + port +
                    ";User Id=" + user +
                    ";Password=" + password;
                var connection = new NpgsqlConnection(connectionString);
                connection.Open();

                if (!isSilentMode) Console.Write("\nPerform ");
                foreach (var filename in files)
                {
	                var errors = String.Empty;

                    if (!isSilentMode) Console.Write((files.IndexOf(filename) + 1) + "/" + files.Count + ": " + filename);
                    var result = isMultiLineMode ? ExecuteMultilineCommand(filename, connection, ref errors) :
	                    ExecuteCommandsBundle(filename, connection, ref errors);

                    // Result notification
                    if (isSilentMode)
                    {
                        if (!result) Console.Write(errors);
                    }
                    else if (result)
                    {
                        Console.Write(" (SUCCESSFUL).");
                    }
                    else
                    {
                        Console.Write(" (ERRORs).");
                        Console.WriteLine("\nReasons:\n" + errors);
                    }
                }

                connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Shows the error message for input arguments errors.
        /// </summary>
        /// <param name="err">The error message.</param>
        private static void ShowError(String err)
        {
            Console.WriteLine(err);
            ShowInfo();
        }

        /// <summary>
        /// Shows the help message to Console.
        /// </summary>
        private static void ShowInfo()
        {
            Console.Write(
                "PgSqlClient 0.4 - PostgreSql .Net Client for executing scripts made in Visual Studio\n\n" +
                "Usage: pgsqlclient [OPTION] file1 file2 ...\n\n" +
                "Options:\n" +
                "  -h, --host=[hostname]      server name or IP address (default: 'localhost')\n" +
                "  -d, --database=[database]  database for connection (default: 'postgres')\n" +
                "  -p, --port=[port]          port number for connection (default: 5432)\n" +
                "  -U, --user=[user]          user name in database (default: 'postgres')\n" +
                "  -P, --password=[password]  user password in database (default: '')\n" +
                "  -m, --multiline            run scripts as multiline command\n" +
                "  -s, --silent               run in silent mode\n" +
                "  --version                  displays information about version of PgSqlClient\n\n" +
                "Inform about bugs to Ilya Urikh <ilya.urikh@gmail.com>.");
        }

        /// <summary>
        /// Executes the script as multiple single-line commands.
        /// </summary>
        /// <param name="filename">The script file name.</param>
        /// <param name="connection">The connection to PostgreSQL server.</param>
        /// <param name="errors">The buffer of error messages.</param>
        /// <returns><c>true</c> if execution was performed successful; otherwise, <c>false</c>.</returns>
        private static Boolean ExecuteCommandsBundle(String filename, NpgsqlConnection connection, ref String errors)
        {
            var parser = new SqlCommandParser(filename);
            var success = true;

            foreach (var sql in parser.Commands)
            {
                try
                {
                    var command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.ExecuteNonQuery();
                }
                catch (NpgsqlException exception)
                {
                    success = false;
                    errors += exception.Message + "\n" + sql + "\n";
                }
            }

            return success;
        }

        /// <summary>
        /// Executes the script as one Multiline command.
        /// </summary>
        /// <param name="filename">The script file name.</param>
        /// <param name="connection">The connection to PostgreSQL server.</param>
        /// <param name="errors">The buffer of error messages.</param>
        /// <returns><c>true</c> if execution was performed successful; otherwise, <c>false</c>.</returns>
        private static Boolean ExecuteMultilineCommand(String filename, NpgsqlConnection connection, ref String errors)
        {
            var result = true;
            String sql;

            using (var sr = File.OpenText(filename))
            {
                sql = sr.ReadToEnd();
            }

            try
            {
                var command = connection.CreateCommand();
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
            catch (NpgsqlException exception)
            {
                result = false;
                errors += exception.Message + "\n";
            }

            return result;
        }
    }
}
