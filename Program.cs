using System;
using System.Collections.Generic;
using System.Text;

namespace OldPhonePadChallenge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Old Phone Pad Test Results ===\n");

            // Test cases from the challenge
            Console.WriteLine("Required Test Cases:");
            TestCase("33#", "E");
            TestCase("227*#", "B");
            TestCase("4433555 555666#", "HELLO");
            TestCase("8 88777444666*664#", "TURING");

            // Additional test cases
            Console.WriteLine("\nAdditional Test Cases:");
            TestCase("#", "");
            TestCase("0#", " ");
            TestCase("222#", "C");
            TestCase("2222#", "A");
            TestCase("22*#", "");
            TestCase("2 2#", "AA");

            // Interactive mode
            Console.WriteLine("\n=== Interactive Mode ===");
            Console.WriteLine("Enter your own test input (or 'exit' to quit):");

            while (true)
            {
                Console.Write("\nInput: ");
                string input = Console.ReadLine();

                if (input.ToLower() == "exit")
                    break;

                string result = OldPhonePad(input);
                Console.WriteLine($"Output: {result}");
            }
        }

        private static void TestCase(string input, string expected)
        {
            string result = OldPhonePad(input);
            bool passed = result == expected;

            Console.WriteLine($"Input: \"{input}\"");
            Console.WriteLine($"Expected: \"{expected}\"");
            Console.WriteLine($"Actual: \"{result}\"");
            Console.WriteLine($"Status: {(passed ? "✓ PASSED" : "✗ FAILED")}");
            Console.WriteLine();
        }

        // This is the method required by the challenge
        public static string OldPhonePad(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove the trailing # if present
            if (input.EndsWith("#"))
                input = input.Substring(0, input.Length - 1);

            StringBuilder result = new StringBuilder();
            int i = 0;

            while (i < input.Length)
            {
                char currentChar = input[i];

                // Handle space (pause between same button presses)
                if (currentChar == ' ')
                {
                    i++;
                    continue;
                }

                // Handle backspace
                if (currentChar == '*')
                {
                    if (result.Length > 0)
                        result.Length--; // Remove last character
                    i++;
                    continue;
                }

                // Count consecutive presses of the same button
                if (KeypadMapping.ContainsKey(currentChar))
                {
                    int pressCount = 1;
                    int j = i + 1;

                    // Count how many times the same button is pressed consecutively
                    while (j < input.Length && input[j] == currentChar)
                    {
                        pressCount++;
                        j++;
                    }

                    // Get the corresponding character based on press count
                    string chars = KeypadMapping[currentChar];
                    if (chars.Length > 0)
                    {
                        // Use modulo to cycle through characters if pressed more times than available chars
                        int charIndex = (pressCount - 1) % chars.Length;
                        result.Append(chars[charIndex]);
                    }

                    // Move index to after all consecutive presses
                    i = j;
                }
                else
                {
                    // Skip unknown characters
                    i++;
                }
            }

            return result.ToString();
        }

        // Dictionary mapping each button to its corresponding characters
        private static readonly Dictionary<char, string> KeypadMapping = new Dictionary<char, string>
        {
            { '0', " " },
            { '1', "&'(" },
            { '2', "ABC" },
            { '3', "DEF" },
            { '4', "GHI" },
            { '5', "JKL" },
            { '6', "MNO" },
            { '7', "PQRS" },
            { '8', "TUV" },
            { '9', "WXYZ" }
        };
    }
}
