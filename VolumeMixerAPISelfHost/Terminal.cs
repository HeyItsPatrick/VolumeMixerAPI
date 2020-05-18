using System;
using System.Runtime.InteropServices;

namespace VolumeMixerAPISelfHost
{
    public class Terminal
    {
        const int ERROR_TOP = 33;
        const int OUTPUT_TOP = 4;
        const int OUTPUT_COLUMN = 67;
        const int OUTPUT_LINES = 30;
        const int WINDOW_HEIGHT = 40;
        const int WINDOW_WIDTH = 120;
        private static int currentErrorRow { get; set; }
        private static int currentOutputRow { get; set; }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void Build()
        {
            //Format Console Window
            Console.WindowHeight = WINDOW_HEIGHT;
            Console.WindowWidth = WINDOW_WIDTH;
            Console.CursorVisible = false;

            //Set so nothing in the Console can be selected
            //Leaving in Quick-Edit hangs up the API and causes errors if anything is selected on the Console window
            IntPtr consoleHandle = GetStdHandle(-10);
            // get current console mode
            uint consoleMode;
            if (GetConsoleMode(consoleHandle, out consoleMode))
            {
                //Got Console mode
                // Clear the quick edit bit in the mode flags
                consoleMode &= ~((uint)0x0040);
                // set the new mode
                SetConsoleMode(consoleHandle, consoleMode);
            }

            for (int i = 0; i < WINDOW_HEIGHT; i++)
            {
                Console.SetCursorPosition(OUTPUT_COLUMN - 2, i);
                Console.Write("|");
            }
            for (int i = 0; i < OUTPUT_COLUMN - 2; i++)
            {
                Console.SetCursorPosition(i, ERROR_TOP - 3);
                Console.Write("-");
                Console.SetCursorPosition(i, ERROR_TOP - 1);
                Console.Write("-");
            }
            Console.SetCursorPosition(0, ERROR_TOP - 2);
            Console.Write("\t\t\tErrors");
            currentErrorRow = ERROR_TOP;
            currentOutputRow = OUTPUT_TOP;
            Console.SetCursorPosition(OUTPUT_COLUMN, 0);
            Console.Write("\t\tAPI is running.");
            Console.SetCursorPosition(OUTPUT_COLUMN, 1);
            Console.Write("Close the window or press CTRL+C to quit.");
            Console.SetCursorPosition(0, WINDOW_HEIGHT - 1);
        }

        public static void PrintError(string value)
        {
            if (currentErrorRow == ERROR_TOP + 5)
            {
                Console.MoveBufferArea(0, ERROR_TOP + 4, OUTPUT_COLUMN - 3, 1, 0, ERROR_TOP);
                currentErrorRow = ERROR_TOP + 1;
                Console.SetCursorPosition(0, currentErrorRow);
                for (int i = 0; i < OUTPUT_COLUMN - 2; i++)
                {
                    Console.SetCursorPosition(i, ERROR_TOP + 1);
                    Console.Write(" ");
                    Console.SetCursorPosition(i, ERROR_TOP + 2);
                    Console.Write(" ");
                    Console.SetCursorPosition(i, ERROR_TOP + 3);
                    Console.Write(" ");
                    Console.SetCursorPosition(i, ERROR_TOP + 4);
                    Console.Write(" ");
                }
            }
            Console.SetCursorPosition(0, currentErrorRow);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write(value);
            Console.ResetColor();
            if (currentErrorRow != ERROR_TOP + 5)
                currentErrorRow++;
            Console.SetCursorPosition(0, WINDOW_HEIGHT - 1);
        }

        public static void PrintOutput(string value)
        {
            if (currentOutputRow == OUTPUT_LINES + OUTPUT_TOP)
            {
                Console.MoveBufferArea(OUTPUT_COLUMN, OUTPUT_TOP + 1, WINDOW_WIDTH - OUTPUT_COLUMN, OUTPUT_LINES, OUTPUT_COLUMN, OUTPUT_TOP);
                currentOutputRow--;
            }
            Console.SetCursorPosition(OUTPUT_COLUMN, currentOutputRow);
            Console.Write(value);
            currentOutputRow++;
            Console.SetCursorPosition(0, WINDOW_HEIGHT - 1);
        }
    }
}
