using System;

namespace SubSolution.CommandLine.Utils
{
    static public class CommandLineUtils
    {
        static public bool AskUserValidation(string question)
        {
            Console.Write(question + " (y/n): ");

            char answer;
            do
            {
                answer = char.ToLower(Convert.ToChar(Console.Read()));
            }
            while (answer != 'y' && answer != 'n');

            return answer == 'y';
        }
    }
}