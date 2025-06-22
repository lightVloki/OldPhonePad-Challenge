using System;
using System.Collections.Generic;
using System.Text;

namespace OldPhonePadChallenge
{
    public static class PhoneKeypadDecoder
    {
        #region Keypad Configuration
        
        /// <summary>
        /// Maps each numeric button to its character sequence.
        /// Design decision: Using a static readonly dictionary provides O(1) lookup
        /// while preventing accidental modification of the keypad layout.
        /// </summary>
        private static readonly IReadOnlyDictionary<char, string> ButtonToCharacters = 
            new Dictionary<char, string>
            {
                ['0'] = " ",      // Space character
                ['1'] = "&'(",    // Special characters
                ['2'] = "ABC",    // First letter group
                ['3'] = "DEF",
                ['4'] = "GHI",
                ['5'] = "JKL",
                ['6'] = "MNO",
                ['7'] = "PQRS",   // Note: 4 characters on 7 and 9
                ['8'] = "TUV",
                ['9'] = "WXYZ"
            };

        #endregion

        #region Constants

        /// <summary>
        /// Character that triggers deletion of the previous character.
        /// In old phones, this was typically the * key.
        /// </summary>
        private const char BACKSPACE_KEY = '*';

        /// <summary>
        /// Character that indicates the end of input sequence.
        /// In old phones, this was the # key used to "send" the message.
        /// </summary>
        private const char SEND_KEY = '#';

        /// <summary>
        /// Character that represents a timing pause between button presses.
        /// This allows the same button to be pressed multiple times for different letters.
        /// Example: "2 2" produces "AA" while "22" produces "B"
        /// </summary>
        private const char TIMING_DELIMITER = ' ';

        #endregion

        #region Public API

        /// <summary>
        /// Converts a sequence of button presses into the corresponding text.
        /// This method implements the core T9 decoding algorithm.
        /// 
        /// Algorithm Overview:
        /// 1. Validate input
        /// 2. Remove terminal send key if present
        /// 3. Process each character sequence:
        ///    - Skip timing delimiters
        ///    - Execute backspace commands
        ///    - Convert button sequences to characters
        /// 
        /// Time Complexity: O(n) where n is the length of the input string
        /// Space Complexity: O(n) for the output string builder
        /// </summary>
        /// <param name="input">
        /// The input sequence containing:
        /// - Digits (0-9): Button presses
        /// - Spaces: Timing delimiters
        /// - Asterisks: Backspace commands
        /// - Hash: End-of-input marker
        /// </param>
        /// <returns>The decoded text string</returns>
        /// <example>
        /// OldPhonePad("2 2#") returns "AA"
        /// OldPhonePad("222#") returns "C"
        /// </example>
        public static string OldPhonePad(string input)
        {
            // Guard clause: Handle null or empty input
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Remove the terminal send key to simplify processing
            string processableInput = RemoveTerminalSendKey(input);

            // Use StringBuilder for efficient string concatenation
            // Pre-allocate capacity based on worst-case scenario (each char = one output)
            var outputBuilder = new StringBuilder(capacity: processableInput.Length);

            // Process the input character by character
            int currentPosition = 0;
            while (currentPosition < processableInput.Length)
            {
                char currentChar = processableInput[currentPosition];

                // Case 1: Timing delimiter - skip it
                if (currentChar == TIMING_DELIMITER)
                {
                    currentPosition++;
                    continue;
                }

                // Case 2: Backspace command - remove last character
                if (currentChar == BACKSPACE_KEY)
                {
                    ExecuteBackspace(outputBuilder);
                    currentPosition++;
                    continue;
                }

                // Case 3: Button press - decode to character
                if (IsValidButton(currentChar))
                {
                    // Count consecutive presses and decode
                    int pressCount = CountConsecutivePresses(processableInput, currentPosition);
                    char decodedChar = DecodeButtonPress(currentChar, pressCount);
                    outputBuilder.Append(decodedChar);
                    
                    // Advance position past all consecutive presses
                    currentPosition += pressCount;
                }
                else
                {
                    // Skip unrecognized characters (defensive programming)
                    currentPosition++;
                }
            }

            return outputBuilder.ToString();
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Removes the terminal send key (#) from the input if present.
        /// This simplifies the main processing loop.
        /// </summary>
        private static string RemoveTerminalSendKey(string input)
        {
            // Check if the last character is the send key
            if (input.Length > 0 && input[input.Length - 1] == SEND_KEY)
            {
                return input.Substring(0, input.Length - 1);
            }
            return input;
        }

        /// <summary>
        /// Checks if a character represents a valid button on the keypad.
        /// </summary>
        private static bool IsValidButton(char character)
        {
            return ButtonToCharacters.ContainsKey(character);
        }

        /// <summary>
        /// Counts how many times a button is pressed consecutively starting from the given position.
        /// This is crucial for determining which character the user intended.
        /// 
        /// Example: In "2223", starting at position 0, this returns 3 (three 2's)
        /// </summary>
        private static int CountConsecutivePresses(string input, int startPosition)
        {
            char targetButton = input[startPosition];
            int count = 1;
            int checkPosition = startPosition + 1;

            // Count while we have the same button and haven't hit end of string
            while (checkPosition < input.Length && input[checkPosition] == targetButton)
            {
                count++;
                checkPosition++;
            }

            return count;
        }

        /// <summary>
        /// Converts a button and press count into the corresponding character.
        /// Uses modular arithmetic to handle "wraparound" when press count exceeds
        /// the number of available characters.
        /// 
        /// Mathematical insight: If a button has 3 characters (A, B, C):
        /// - 1 press = A (index 0)
        /// - 2 presses = B (index 1)
        /// - 3 presses = C (index 2)
        /// - 4 presses = A (wraps back to index 0)
        /// 
        /// This is achieved using: (pressCount - 1) % characterCount
        /// </summary>
        private static char DecodeButtonPress(char button, int pressCount)
        {
            string availableCharacters = ButtonToCharacters[button];
            
            // Handle edge case: button with no characters (shouldn't happen with our mapping)
            if (availableCharacters.Length == 0)
            {
                return '\0'; // Null character
            }

            // Calculate the character index using modular arithmetic
            // Subtract 1 because pressCount is 1-based but array indices are 0-based
            int characterIndex = (pressCount - 1) % availableCharacters.Length;
            
            return availableCharacters[characterIndex];
        }

        /// <summary>
        /// Executes a backspace operation by removing the last character from the builder.
        /// Safely handles the case where the builder is empty.
        /// </summary>
        private static void ExecuteBackspace(StringBuilder builder)
        {
            if (builder.Length > 0)
            {
                builder.Length--; // Efficient way to remove last character
            }
            // If builder is empty, backspace has no effect (matches real phone behavior)
        }

        #endregion
    }

    /// <summary>
    /// Console application to demonstrate and test the OldPhonePad functionality.
    /// Provides both automated testing and interactive mode for exploration.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== Old Phone Pad Decoder ===");
            Console.WriteLine("Engineering demonstration by [Your Name]\n");

            RunAutomatedTests();
            RunInteractiveMode();
        }

        /// <summary>
        /// Runs a comprehensive suite of automated tests to verify correctness.
        /// Tests are organized by category for clarity.
        /// </summary>
        private static void RunAutomatedTests()
        {
            Console.WriteLine("Running Automated Test Suite...\n");

            // Category 1: Basic functionality tests
            Console.WriteLine("Category 1: Basic Functionality");
            RunTest("33#", "E", "Single button, multiple presses");
            RunTest("227*#", "B", "Backspace functionality");
            RunTest("4433555 555666#", "HELLO", "Space delimiter between same button");
            RunTest("8 88777444666*664#", "TURING", "Complex sequence with backspace");

            // Category 2: Edge cases
            Console.WriteLine("\nCategory 2: Edge Cases");
            RunTest("#", "", "Empty input (only send key)");
            RunTest("", "", "Null input");
            RunTest("***#", "", "Multiple backspaces on empty string");
            RunTest("0#", " ", "Space character");
            RunTest("00#", " ", "Multiple spaces collapse to one");

            // Category 3: Wraparound behavior
            Console.WriteLine("\nCategory 3: Character Cycling (Wraparound)");
            RunTest("2222#", "A", "Wraparound on 3-character button");
            RunTest("77777#", "P", "Wraparound on 4-character button");
            RunTest("222222#", "C", "Double wraparound");

            // Category 4: Special characters
            Console.WriteLine("\nCategory 4: Special Characters");
            RunTest("1#", "&", "First special character");
            RunTest("111#", "(", "Last special character");
            RunTest("1111#", "&", "Special character wraparound");

            // Category 5: Complex scenarios
            Console.WriteLine("\nCategory 5: Real-World Scenarios");
            RunTest("44 444#", "GI", "Common word fragment");
            RunTest("666 66#", "ON", "Another common fragment");
            RunTest("9999 666 777 5553#", "WORLD", "Full word with varied press counts");

            Console.WriteLine("\n" + new string('=', 50) + "\n");
        }

        /// <summary>
        /// Executes a single test case and reports the result.
        /// Provides detailed output for debugging and verification.
        /// </summary>
        private static void RunTest(string input, string expected, string description)
        {
            string actual = PhoneKeypadDecoder.OldPhonePad(input);
            bool passed = actual == expected;
            
            Console.WriteLine($"Test: {description}");
            Console.WriteLine($"  Input:    \"{input}\"");
            Console.WriteLine($"  Expected: \"{expected}\"");
            Console.WriteLine($"  Actual:   \"{actual}\"");
            Console.WriteLine($"  Result:   {(passed ? "✓ PASS" : "✗ FAIL")}");
            
            if (!passed)
            {
                Console.WriteLine($"  ERROR: Expected '{expected}' but got '{actual}'");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Provides an interactive mode for manual testing and exploration.
        /// Useful for understanding the algorithm's behavior with custom inputs.
        /// </summary>
        private static void RunInteractiveMode()
        {
            Console.WriteLine("=== Interactive Mode ===");
            Console.WriteLine("Enter sequences to decode. Type 'help' for usage or 'exit' to quit.\n");

            while (true)
            {
                Console.Write("Input: ");
                string input = Console.ReadLine()?.Trim() ?? "";

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Thank you for using the Old Phone Pad Decoder!");
                    break;
                }

                if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
                {
                    ShowHelp();
                    continue;
                }

                try
                {
                    string result = PhoneKeypadDecoder.OldPhonePad(input);
                    Console.WriteLine($"Output: \"{result}\"\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}\n");
                }
            }
        }

        /// <summary>
        /// Displays usage instructions for the interactive mode.
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("\n=== HELP ===");
            Console.WriteLine("Button Mappings:");
            Console.WriteLine("  0: [space]");
            Console.WriteLine("  1: & ' (");
            Console.WriteLine("  2: A B C");
            Console.WriteLine("  3: D E F");
            Console.WriteLine("  4: G H I");
            Console.WriteLine("  5: J K L");
            Console.WriteLine("  6: M N O");
            Console.WriteLine("  7: P Q R S");
            Console.WriteLine("  8: T U V");
            Console.WriteLine("  9: W X Y Z");
            Console.WriteLine("\nSpecial Keys:");
            Console.WriteLine("  * : Backspace (delete last character)");
            Console.WriteLine("  # : Send (end of input)");
            Console.WriteLine("  [space] : Pause (to type same button again)");
            Console.WriteLine("\nExample: '4433555 555666#' → 'HELLO'");
            Console.WriteLine("=============\n");
        }
    }
}
