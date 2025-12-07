using System;
using System.Threading.Tasks;

class Program
{
    public static async Task Main(string[] args)
    {
        /*
        -l --login
        -sc --set-contest <constest_id>
        -lc --list-contests
        -st --set-task <task_index>
        -lt --list-tasks
        -ll --list-compilers
        -sl --set-compiler <compiler_index>
        -s --submit <file_path>
        -ls --list-submits
        -h --help
        */
        var ts = new TsWebCli();
        for (int arg_ind = 0; arg_ind < args.Length; ++arg_ind)
        {
            string type = args[arg_ind];
            try{
                switch (type)
                {
                    case "-d":
                    case "--debug":
                        ts.debug=true;
                        break;
                    case "-l":
                    case "--login":
                        await ts.Login();
                        break;
                    case "-sc":
                    case "--set-contest":
                        await ts.SetContest(args[1+arg_ind++]);
                        break;
                    case "-lc":
                    case "--list-contests":
                        await ts.ListContests();
                        break;
                    case "-st":
                    case "--set-task":
                        ts.SetTask(args[1+arg_ind++]);
                        break;
                    case "-lt":
                    case "--list-tasks":
                        await ts.ListTasks();
                        break;
                    case "-sl":
                    case "--set-compiler":
                        ts.SetCompiler(args[1+arg_ind++]);
                        break;
                    case "-ll":
                    case "--list-compilers":
                        await ts.ListCompilers();
                        break;
                    case "-s":
                    case "--submit":
                        await ts.Submit(args[1+arg_ind++]);
                        break;
                    case "-ls":
                    case "--list-submits":
                        await ts.ListSubmits();
                        break;
                    default:
                        Console.WriteLine($"Unresoled argument: {args[arg_ind]}.");
                        return;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }
        }
        ts.Dispose();
    }
}
