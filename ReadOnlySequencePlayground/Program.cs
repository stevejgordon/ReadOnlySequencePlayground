using System;
using System.Buffers;

namespace ReadOnlySequencePlayground
{
    internal class Program
    {
        private const int MaxStackLength = 32; // 128 bytes

        private static void Main()
        {
            var arrayOne = new[] { 0, 1, 2, 3, 4 };
            var arrayTwo = new[] { 5, 6, 7, 8, 9 };
            var arrayThree = new[] { 10, 11, 12, 13, 14 };

            // Create linked segments
            var first = new MemorySegment<int>(arrayOne);
            var second = first.Append(arrayTwo);
            var last = second.Append(arrayThree);
            
            // Create the sequence, passing first and last segments
            var ros = new ReadOnlySequence<int>(first, 0, last, last.Memory.Length);

            ParseExampleOne(ros);
            ParseExampleTwo(ros);
        }

        private static void ParseExampleOne(ReadOnlySequence<int> ros)
        {
            var sequenceReader = new SequenceReader<int>(ros);

            var sequenceLength = Convert.ToInt32(sequenceReader.Length); // may throw for large sequences

            Span<int> output = sequenceLength < MaxStackLength
                ? stackalloc int[sequenceLength] // For small amounts of data we optimise to the stack for the output
                : new int[sequenceLength]; // This allocates and could be improved using ArrayPool<T>

            var position = 0;

            // Same effect as above, but more verbose
            if (sequenceReader.TryAdvanceTo(6, advancePastDelimiter: false))
            {
                while (!sequenceReader.End)
                {
                    if (sequenceReader.TryRead(out var value))
                    {
                        output[position++] = value; // copy data to the output buffer
                    }
                }
            }

            var finalData = output.Slice(0, position);

            foreach (var value in finalData)
            {
                Console.WriteLine(value);
            }
        }

        private static void ParseExampleTwo(ReadOnlySequence<int> ros)
        {
            var sequenceReader = new SequenceReader<int>(ros);

            //Try to find and start reading from '6' onward

            if (!sequenceReader.TryAdvanceTo(6, advancePastDelimiter: false)) return;
            
            var remaining = sequenceReader.Sequence.Slice(sequenceReader.Position);
            var length = Convert.ToInt32(remaining.Length); // may throw for large sequences

            Span<int> output = length < MaxStackLength
                ? stackalloc int[length] // For small amounts of data we optimise to the stack for the output
                : new int[length]; // This allocates and could be improved using ArrayPool<T>

            remaining.CopyTo(output);

            foreach (var value in output)
            {
                Console.WriteLine(value);
            }
        }
    }

    internal class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory)
        {
            Memory = memory;
        }

        public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;

            return segment;
        }
    }
}
