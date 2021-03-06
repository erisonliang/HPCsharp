﻿// TODO: Write a technical paper on RadixSort2, with it's new method to improve memory access pattern of Radix Sort, which is especially affective when sorting
//       arrays or user defined classes, which use references and thus can be scattered all over the heap. Measurements are showing 10X performance improvement.
// TODO: Figure out how to end RadixSort early for those cases where the keys being sorted are within a limited range, such as for keys in a database - e.g. fewer than 16 M keys which are 0 to 16M
//       which is within 24-bits the lower bits.
// TODO: Try Radix Sort by processing 4-bits at a time to reduce random memory access pattern.
// TODO: To reduce random memory access pattern develop a multi-buffer class (maybe) that you write thru, which has multiple buffers that you write to, and then automatically
//       flushes them to memory when the individual buffer size reaches its limit. This turns random memory accesses into sequential memory accesses. It also allows
//       to flush all of the buffers at any time. This is a similar strategy to what Intel used to speed up Radix Sort. Then play with the buffer size - i.e. is it cache
//       line size or bigger. This would be a class that you write thru. You would set the address of each destination for each buffer, and then you would write data thru
//       individual buffer to system memory.
// TODO: Make all multi-dimensional buffers a single array, to keep cache usage and mapping to the same cache location impossible.
// TODO: Pull out and measure the counting portion of the algorithm, as I've done with the serial algorithm.
// TODO: For parallel algorithm, parallel the counting portion of the algorithm, as I've done in my Dr. Dobb's papers for the parallel counting sort and MSD Radix Sort.
//       To start with don't parallel the permuting portion of the algorithm until we figure out how to do it with a performance gain.
// TODO: Bring in parallel Quick Sort from the link that John provided.
// TODO: Figure out why for pre-sorted arrays of uint Radix Sort's 4 passes are much slower on the middle two passes than on the first and last pass. The last pass is
//       understandable since most likely all elements go into a single bin and that's why it's fast. This could be an opportunity to expose a weakness in this algorithm
//       or an opportunity to fix this weakness.
// TODO: Change the count array into a 1-D array to minimize cache contention, since 2-D array gets allocated one row at a time and may cache interfere between rows,
//       depending on how each row gets allocated. With 1-D the memory layout is guaranteed to be contigous, which should produce less cache contention.
//       Do the same with startOfBin 2-D array. It'll be a little bit more painful to use, but performance gains should make it worthwhile.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HPCsharp
{
    static public partial class Algorithm
    {
        /// <summary>
        /// Sort an array of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        private static uint[] SortRadix3(this uint[] inputArray)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            uint[] outputArray = new uint[inputArray.Length];
            uint[] count       = new uint[numberOfBins];
            uint[] startOfBin  = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++;

                startOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++) // Victor J. Duvanenko
                    startOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                    outputArray[startOfBin[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                uint[] tmp  = inputArray;       // swap input and output arrays
                inputArray  = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        // For now, it's fixed at 8-bits per digit. We can generalize later.
        private static void ExtractDigits(UInt32 value, uint[] digits)
        {
            digits[0] = (value &       0xff);
            digits[1] = (value &     0xff00) >> 8;
            digits[2] = (value &   0xff0000) >> 16;
            digits[3] = (value & 0xff000000) >> 24;
        }
        /// <summary>
        /// Sort an array of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        private static uint[] SortRadix2(this uint[] inputArray)
        {
            int numberOfBins = 256;
            int numberOfDigits = 4;
            int Log2ofPowerOfTwoRadix = 8;
            int d = 0;
            uint[] outputArray = new uint[inputArray.Length];

            uint[][] count = new uint[numberOfDigits][];
            for (int i = 0; i < numberOfDigits; i++)
                count[i] = new uint[numberOfBins];
            uint[][] startOfBin = new uint[numberOfDigits][];
            for (int i = 0; i < numberOfDigits; i++)
                startOfBin[i] = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            //Stopwatch stopwatch = new Stopwatch();
            //long frequency = Stopwatch.Frequency;
            ////Console.WriteLine("  Timer frequency in ticks per second = {0}", frequency);
            //long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;

            //stopwatch.Restart();
            var digits = new byte[4];
            for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
            {
                uint value = inputArray[current];
                digits[0] = (byte)(value & 0xff);
                digits[1] = (byte)((value & 0xff00) >> 8);
                digits[2] = (byte)((value & 0xff0000) >> 16);
                digits[3] = (byte)((value & 0xff000000) >> 24);

                int i = 0;
                foreach(var digit in digits)
                {
                    count[i][digit]++;
                    i++;
                }
            }
            //stopwatch.Stop();
            //double timeForCounting = stopwatch.ElapsedTicks * nanosecPerTick / 1000000000.0;
            //Console.WriteLine("Time for counting: {0}", timeForCounting);

            for (d = 0; d < numberOfDigits; d++)
            {
                startOfBin[d][0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[d][i] = startOfBin[d][i - 1] + count[d][i - 1];
            }

            d = 0;
            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                //stopwatch.Restart();
                uint[] startOfBinLoc = startOfBin[d];
                for (uint current = 0; current < inputArray.Length; current++)
                {
                    //outputArray[startOfBinLoc[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++] = inputArray[current];
                    outputArray[startOfBinLoc[(inputArray[current] & bitMask) >> shiftRightAmount]++] = inputArray[current];
                }
                //stopwatch.Stop();
                //double timeForPermuting = stopwatch.ElapsedTicks * nanosecPerTick / 1000000000.0;
                //Console.WriteLine("Time for permuting: {0}", timeForPermuting);

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;
                d++;

                uint[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        [StructLayout(LayoutKind.Explicit)]
        struct UInt32ByteUnion
        {
            [FieldOffset(0)]
            public byte byte0;
            [FieldOffset(1)]
            public byte byte1;
            [FieldOffset(2)]
            public byte byte2;
            [FieldOffset(3)]
            public byte byte3;

            [FieldOffset(0)]
            public UInt32 integer;
        }
        /// <summary>
        /// Sort an array of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        public static uint[] SortRadix(this uint[] inputArray)
        {
            int numberOfBins = 256;
            int numberOfDigits = 4;
            int Log2ofPowerOfTwoRadix = 8;
            int d = 0;
            uint[] outputArray = new uint[inputArray.Length];

            uint[][] count = new uint[numberOfDigits][];
            for (int i = 0; i < numberOfDigits; i++)
                count[i] = new uint[numberOfBins];
            uint[][] startOfBin = new uint[numberOfDigits][];
            for (int i = 0; i < numberOfDigits; i++)
                startOfBin[i] = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            //Stopwatch stopwatch = new Stopwatch();
            //long frequency = Stopwatch.Frequency;
            ////Console.WriteLine("  Timer frequency in ticks per second = {0}", frequency);
            //long nanosecPerTick = (1000L * 1000L * 1000L) / frequency;

            //stopwatch.Restart();
            var digits = new byte[4];
            UInt32ByteUnion union = new UInt32ByteUnion();
            for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
            {
                union.integer = inputArray[current];
                count[0][union.byte0]++;
                count[1][union.byte1]++;
                count[2][union.byte2]++;
                count[3][union.byte3]++;
            }
            //stopwatch.Stop();
            //double timeForCounting = stopwatch.ElapsedTicks * nanosecPerTick / 1000000000.0;
            //Console.WriteLine("Time for counting: {0}", timeForCounting);

            for (d = 0; d < numberOfDigits; d++)
            {
                startOfBin[d][0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[d][i] = startOfBin[d][i - 1] + count[d][i - 1];
            }

            d = 0;
            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                //stopwatch.Restart();
                uint[] startOfBinLoc = startOfBin[d];
                for (uint current = 0; current < inputArray.Length; current++)
                {
                    //outputArray[startOfBinLoc[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++] = inputArray[current];
                    outputArray[startOfBinLoc[(inputArray[current] & bitMask) >> shiftRightAmount]++] = inputArray[current];
                }
                //stopwatch.Stop();
                //double timeForPermuting = stopwatch.ElapsedTicks * nanosecPerTick / 1000000000.0;
                //Console.WriteLine("Time for permuting: {0}", timeForPermuting);

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;
                d++;

                uint[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        /// <summary>
        /// Sort an array of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        public static void SortRadix(this uint[] inputArray, int startIndex, int length, uint[] tmpArray)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            int[] count       = new int[numberOfBins];
            int[] startOfBin  = new int[numberOfBins];
            bool tmpArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (int current = startIndex; current < (startIndex + length); current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++;

                startOfBin[0] = startIndex;
                for (uint i = 1; i < numberOfBins; i++) // Victor J. Duvanenko
                    startOfBin[i] = (startOfBin[i - 1] + count[i - 1]);

                for (int current = startIndex; current < (startIndex + length); current++)
                    tmpArray[startOfBin[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                tmpArrayHasResult = !tmpArrayHasResult;

                uint[] tmp = inputArray;       // swap input and tmp arrays
                inputArray = tmpArray;
                tmpArray = tmp;
            }
            uint[] tmp1 = inputArray;       // swap input and tmp arrays
            inputArray = tmpArray;
            tmpArray = tmp1;
            //if (outputArrayHasResult)
            //    for (int current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
            //        inputArray[current] = outputArray[current];

            //return inputArray;
        }
        /// <summary>
        /// Sort an List of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        public static List<uint> SortRadix(this List<uint> inputArray)
        {
            var srcCopy = inputArray.ToArray();
            var sortedArray = srcCopy.SortRadix();
            var sortedList = new List<uint>(sortedArray);
            return sortedList;
        }
        /// <summary>
        /// Sort an array of unsigned integers using Radix Sorting algorithm (least significant digit variation)
        /// </summary>
        /// <param name="inputArray"></param>
        /// <returns>array of unsigned integers</returns>
        public static uint[] SortRadix4(this uint[] inputArray)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            uint cacheLineSizeInBytes = 64 * 4;
            uint cacheLineSizeInUInt32s = cacheLineSizeInBytes / 4;  // is there sizeOf() in C#
            uint[] cacheBuffers = new uint[numberOfBins * cacheLineSizeInUInt32s];
            uint[] cacheBufferIndexes = new uint[numberOfBins];
            uint[] outputArray = new uint[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                {
                    count[i] = 0;
                    cacheBufferIndexes[i] = 0;
                }
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                {
                    uint whichBin = ExtractDigit(inputArray[current], bitMask, shiftRightAmount);
                    uint startOfBuffer = whichBin * cacheLineSizeInUInt32s;
                    if (cacheBufferIndexes[whichBin] < cacheLineSizeInUInt32s)  // place current element into its cacheBuffer
                    {
                        cacheBuffers[startOfBuffer + cacheBufferIndexes[whichBin]] = inputArray[current];
                        cacheBufferIndexes[whichBin]++;
                    }
                    else     // flush the buffer to system memory
                    {
                        uint index = startOfBuffer;
                        for (int i = 0; i < cacheLineSizeInUInt32s; i++)
                            outputArray[endOfBin[whichBin]++] = cacheBuffers[index++];
                        cacheBuffers[startOfBuffer] = inputArray[current];
                        cacheBufferIndexes[whichBin] = 1;
                    }
                    //outputArray[endOfBin[ExtractDigit(inputArray[current], bitMask, shiftRightAmount)]++] = inputArray[current];
                }
                // Flush all of the cache buffers
                for (uint whichBin = 0; whichBin < numberOfBins; whichBin++)
                {
                    uint startOfBuffer = whichBin * cacheLineSizeInUInt32s;
                    uint index = startOfBuffer;
                    uint currentIndex = cacheBufferIndexes[whichBin];
                    for (int i = 0; i < currentIndex; i++)
                        outputArray[endOfBin[whichBin]++] = cacheBuffers[index++];
                }

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                uint[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="getKey">user provided function to extract the unsigned 32-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix<T>(this T[] inputArray, Func<T, UInt32> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[] outputArray = new T[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                    outputArray[endOfBin[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="getKey">user provided function to extract the unsigned 64-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix<T>(this T[] inputArray, Func<T, UInt64> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[] outputArray = new T[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                    outputArray[endOfBin[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                    inputArray[current] = outputArray[current];

            return inputArray;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="getKey">user provided function to extract the unsigned 64-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static void SortRadix<T>(this T[] inputArray, Int32 start, Int32 length, T[] outputArray, Func<T, UInt32> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);
                for (uint i = 1; i < numberOfBins; i++)
                {
                    startOfBin[i] += (uint)start;
                    endOfBin[  i] += (uint)start;
                }

                for (uint current = 0; current < inputArray.Length; current++)
                    outputArray[endOfBin[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (!outputArrayHasResult)
                for (int current = start; current < (start + length); current++)
                    outputArray[current] = inputArray[current];
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputArray"></param>
        /// <param name="getKey">user provided function to extract the unsigned 64-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static void SortRadixNew<T>(this T[] inputArray, Int32 start, Int32 length, T[] outputArray, Func<T, UInt32> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan the array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);
                for (uint i = 1; i < numberOfBins; i++)
                {
                    startOfBin[i] += (uint)start;
                    endOfBin[  i] += (uint)start;
                }

                for (uint current = 0; current < inputArray.Length; current++)
                    outputArray[endOfBin[ExtractDigit(getKey(inputArray[current]), bitMask, shiftRightAmount)]++] = inputArray[current];

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;       // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
            }
            if (!outputArrayHasResult)
                for (int current = start; current < (start + length); current++)
                    outputArray[current] = inputArray[current];
        }
        private static UInt32 ExtractDigit(UInt32 value, UInt32 bitMask, int shiftRightAmount)
        {
            return (value & bitMask) >> shiftRightAmount;	// extract the digit we are sorting based on
        }
        private static UInt32 ExtractDigit(UInt64 value, UInt64 bitMask, int shiftRightAmount)
        {
            return (UInt32)((value & bitMask) >> shiftRightAmount);	// extract the digit we are sorting based on
        }
        /// <summary>
        /// Sort a List of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputList"></param>
        /// <param name="getKey">user provided function to extract the unsigned 32-bit key sorted on</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix<T>(this List<T> inputList, Func<T, UInt32> getKey)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix(getKey);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
        /// <summary>
        /// Sort a List of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Slower algorithm that allocates only 3K bytes of extra memory.
        /// </summary>
        /// <param name="inputList"></param>
        /// <param name="getKey">user provided function to extract the unsigned 32-bit key sorted on</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix<T>(this List<T> inputList, Func<T, UInt64> getKey)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix(getKey);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// </summary>
        /// <param name="inputArray">input array of type T</param>
        /// <param name="inKeys">input array of keys (unsigned integers) to be sorted on</param>
        /// <param name="outSortedKeys">sorted array of keys (unsigned integers)</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix<T>(this T[] inputArray, UInt32[] inKeys, ref UInt32[] outSortedKeys)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[] outputArray = new T[inputArray.Length];
            if (outSortedKeys == null || outSortedKeys.Length != inputArray.Length)
                outSortedKeys = new UInt32[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                {
                    uint endOfBinIndex = ExtractDigit(inKeys[current], bitMask, shiftRightAmount);
                    uint index = endOfBin[endOfBinIndex];
                    outputArray[index] = inputArray[current];
                    outSortedKeys[index] = inKeys[current];
                    endOfBin[endOfBinIndex]++;
                }

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;        // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
                UInt32[] tmpKeys = inKeys;   // swap input and output key arrays
                inKeys = outSortedKeys;
                outSortedKeys = tmpKeys;

            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                {
                    inputArray[current] = outputArray[current];
                    inKeys[current] = outSortedKeys[current];
                }

            return inputArray;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// </summary>
        /// <param name="inputArray">input array of type T</param>
        /// <param name="inKeys">input array of keys (unsigned integers) to be sorted on</param>
        /// <param name="outSortedKeys">sorted array of keys (unsigned integers)</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix<T>(this T[] inputArray, UInt64[] inKeys, ref UInt64[] outSortedKeys)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[] outputArray = new T[inputArray.Length];
            if (outSortedKeys == null || outSortedKeys.Length != inputArray.Length)
                outSortedKeys = new UInt64[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                    count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;

                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                {
                    uint endOfBinIndex = ExtractDigit(inKeys[current], bitMask, shiftRightAmount);
                    uint index = endOfBin[endOfBinIndex];
                    outputArray[index] = inputArray[current];
                    outSortedKeys[index] = inKeys[current];
                    endOfBin[endOfBinIndex]++;
                }

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;        // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
                UInt64[] tmpKeys = inKeys;   // swap input and output key arrays
                inKeys = outSortedKeys;
                outSortedKeys = tmpKeys;

            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                {
                    inputArray[current] = outputArray[current];
                    inKeys[current] = outSortedKeys[current];
                }

            return inputArray;
        }
        /// <summary>
        /// Sort a List of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// </summary>
        /// <param name="inputList">input List of type T</param>
        /// <param name="inKeys">input array of keys (unsigned integers) to be sorted on</param>
        /// <param name="outSortedKeys">sorted array of keys (unsigned integers)</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix<T>(this List<T> inputList, UInt32[] inKeys, ref UInt32[] outSortedKeys)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix(inKeys, ref outSortedKeys);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned 64-bit integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// </summary>
        /// <param name="inputList">input List of type T</param>
        /// <param name="inKeys">input array of keys (unsigned integers) to be sorted on</param>
        /// <param name="outSortedKeys">sorted array of keys (unsigned integers)</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix<T>(this List<T> inputList, UInt64[] inKeys, ref UInt64[] outSortedKeys)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix(inKeys, ref outSortedKeys);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
        // The following algorithms use a method where we extract an array of keys and have a source and destination array of keys to go along with
        // array of references so that accesses to keys are more cache friendly, and accesses to references go along with they keys!
        // The keys are in a linear array. The references to objects are in a linear array. Each object is never touched, since objects are scattered
        // in memory, making access to objects slow.
        // This method should be much better for arrays of objects/classes for JavaScript, Java, C#, C++ and Python.

        /// <summary>
        /// Sort an array of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Faster algorithm that uses 8N bytes more temporary memory than SortRadix, where N is the size of the input array.
        /// </summary>
        /// <param name="inputArray">input array of type T</param>
        /// <param name="getKey">user provided function to extract the unsigned 32-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix2<T>(this T[] inputArray, Func<T, UInt32> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[]      outputArray   = new      T[inputArray.Length];
            UInt32[] inKeys        = new UInt32[inputArray.Length];
            UInt32[] outSortedKeys = new UInt32[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin   = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                if (bitMask == 255)     // first pass
                    for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                    {
                        inKeys[current] = getKey(inputArray[current]);
                        count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;
                    }
                else
                    for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                        count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;


                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                {
                    uint endOfBinIndex = ExtractDigit(inKeys[current], bitMask, shiftRightAmount);
                    uint index = endOfBin[endOfBinIndex];
                    outputArray[index] = inputArray[current];
                    outSortedKeys[index] = inKeys[current];
                    endOfBin[endOfBinIndex]++;
                }

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;        // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
                UInt32[] tmpKeys = inKeys;   // swap input and output key arrays
                inKeys = outSortedKeys;
                outSortedKeys = tmpKeys;

            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                {
                    inputArray[current] = outputArray[current];
                    inKeys[current] = outSortedKeys[current];
                }

            return inputArray;
        }
        /// <summary>
        /// Sort an array of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Faster algorithm that uses 16N bytes more temporary memory than SortRadix, where N is the size of the input array.
        /// </summary>
        /// <param name="inputArray">input array of type T</param>
        /// <param name="getKey">user provided function to extract the unsigned 64-bit key sorted on, from the user defined class</param>
        /// <returns>sorted array of user defined class</returns>
        public static T[] SortRadix2<T>(this T[] inputArray, Func<T, UInt64> getKey)
        {
            int numberOfBins = 256;
            int Log2ofPowerOfTwoRadix = 8;
            T[] outputArray = new T[inputArray.Length];
            UInt64[] inKeys = new UInt64[inputArray.Length];
            UInt64[] outSortedKeys = new UInt64[inputArray.Length];
            uint[] count = new uint[numberOfBins];
            bool outputArrayHasResult = false;

            uint bitMask = 255;
            int shiftRightAmount = 0;

            uint[] startOfBin = new uint[numberOfBins];
            uint[] endOfBin = new uint[numberOfBins];

            while (bitMask != 0)    // end processing digits when all the mask bits have been processed and shifted out, leaving no bits set in the bitMask
            {
                for (uint i = 0; i < numberOfBins; i++)
                    count[i] = 0;
                if (bitMask == 255)
                    for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                    {
                        inKeys[current] = getKey(inputArray[current]);
                        count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;
                    }
                else
                    for (uint current = 0; current < inputArray.Length; current++)    // Scan Key array and count the number of times each digit value appears - i.e. size of each bin
                        count[ExtractDigit(inKeys[current], bitMask, shiftRightAmount)]++;


                startOfBin[0] = endOfBin[0] = 0;
                for (uint i = 1; i < numberOfBins; i++)
                    startOfBin[i] = endOfBin[i] = (uint)(startOfBin[i - 1] + count[i - 1]);

                for (uint current = 0; current < inputArray.Length; current++)
                {
                    uint endOfBinIndex = ExtractDigit(inKeys[current], bitMask, shiftRightAmount);
                    uint index = endOfBin[endOfBinIndex];
                    outputArray[index] = inputArray[current];
                    outSortedKeys[index] = inKeys[current];
                    endOfBin[endOfBinIndex]++;
                }

                bitMask <<= Log2ofPowerOfTwoRadix;
                shiftRightAmount += Log2ofPowerOfTwoRadix;
                outputArrayHasResult = !outputArrayHasResult;

                T[] tmp = inputArray;        // swap input and output arrays
                inputArray = outputArray;
                outputArray = tmp;
                UInt64[] tmpKeys = inKeys;   // swap input and output key arrays
                inKeys = outSortedKeys;
                outSortedKeys = tmpKeys;

            }
            if (outputArrayHasResult)
                for (uint current = 0; current < inputArray.Length; current++)    // copy from output array into the input array
                {
                    inputArray[current] = outputArray[current];
                    inKeys[current] = outSortedKeys[current];
                }

            return inputArray;
        }
        /// <summary>
        /// Sort a List of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Faster algorithm that uses 8N bytes more temporary memory than SortRadix, where N is the size of the input array.
        /// </summary>
        /// <param name="inputList">input List of type T</param>
        /// <param name="getKey">user provided function to extract the unsigned 32-bit key sorted on, from the user defined class</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix2<T>(this List<T> inputList, Func<T, UInt32> getKey)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix2(getKey);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
        /// <summary>
        /// Sort a List of user defined class containing an unsigned integer Key, using Radix Sorting algorithm. Linear time sort algorithm.
        /// Faster algorithm that uses 16N bytes more temporary memory than SortRadix, where N is the size of the input array.
        /// </summary>
        /// <param name="inputList">input List of type T</param>
        /// <param name="getKey">user provided function to extract the unsigned 64-bit key sorted on, from the user defined class</param>
        /// <returns>sorted List of user defined class</returns>
        public static List<T> SortRadix2<T>(this List<T> inputList, Func<T, UInt64> getKey)
        {
            var srcCopy = inputList.ToArray();
            var sortedArray = srcCopy.SortRadix2(getKey);
            var sortedList = new List<T>(sortedArray);
            return sortedList;
        }
    }
}
