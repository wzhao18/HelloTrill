﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Microsoft.StreamProcessing;
using Microsoft.StreamProcessing.Aggregates;

namespace HelloTrill
{
    class Program
    {
        static void Main(string[] args)
        {
            /**
             * Generating synthetic data
             */
            var SIZE = 1000; // Size of the data set
            var listA = new List<int>(); // A list for storing the data points
            var listB = new List<int>(); // Another list for storing the data points
            var listC = new List<int>();
            for (int i = 0; i < SIZE; i++)
            {
                listA.Add(i); // Populate listA with dummy data
                listB.Add(i); // Populate listB with dummy data
                if (i > 50 && i < 100 || i > 200 && i < 400) {}
                else { listC.Add(i); }
            }

            /**
             * Creating lists created above to Trill streams
             */
            var streamA = listA // Creating first stream from listA 
                    .ToObservable() // Convert the data list to an Observable first 
                    .ToTemporalStreamable(e => e, e => e + 1) // Then convert to Trill temporal stream;
                ; // nth event in the stream has an integer payload 'n'
            // and an interval of [n, n+1)

            var streamB = listB // Creating streamB (not using yet) similar to streamA.
                    .ToObservable()
                    .ToTemporalStreamable(e => e, e => e + 100)
                ;
            
            var streamC = listC // Creating streamC, which has gaps
                    .ToObservable()
                    .ToTemporalStreamable(e => e, e => e + 1)
                ;
            
            // 1.1
            var level1_1 = streamA
                .Select(e => e + 1);

            var level1_2 = streamA
                .Select(e => 3 * e);
            
            var level1_3 = streamA
                .Where(e => e % 2 == 1);

            var level1_4 = streamA
                .Select(e => 3 * (e + 1)) 
                .Where(e => e % 2 == 1);

            var level2_1 = streamA
                .ShiftEventLifetime(1)
                .TumblingWindowLifetime(10)
                .ShiftEventLifetime(-10)
                .Sum(e => e);
            
            var level2_2 = streamA
                .Join(streamB, (A, B) => A + B);
            
            var level2_3 = streamA.
                Join(streamB, (A, B) => new {A, B});

            var level2_4 = streamB
                .ShiftEventLifetime(1)
                .TumblingWindowLifetime(10)
                .ShiftEventLifetime(-10)
                .Sum(e => e);

            var level2_5 = streamA.Multicast(s => s
                .ShiftEventLifetime(1)
                .TumblingWindowLifetime(10)
                .ShiftEventLifetime(-10)
                .Sum(e => e)
                .Join(s, (l, r) => new {l, r})
            );

            var level2_6 = streamA
                .Multicast(a => a
                    .ShiftEventLifetime(1)
                    .TumblingWindowLifetime(10)
                    .ShiftEventLifetime(-10)
                    .Multicast(s => s.Average(e => e).Join(s
                            .Aggregate(w => w.StandardDeviation(e => e)),
                        (avg, sd) => new {Avg = avg, Sd = sd}))
                    .Join(a, (l, r) => (r - l.Avg) / l.Sd));

            // Something really weird happened on this one, don't know why Sum turns intervals into start and end 
            var level2dot5 = streamA
                .ShiftEventLifetime(1)
                .HoppingWindowLifetime(10, 5)
                .ShiftEventLifetime(-5)
                .Sum(e => e);

            var level3_1 = streamC
                .AlterEventDuration(StreamEvent.InfinitySyncTime)
                .Multicast(s => s
                    .ClipEventDuration(s));

            /* This query depends on level 3.1. Fixing 3.1 will make the following work
            var level3_2 = level3_1
                .Chop(0, 1);
                
            */

            /*
            var level3_3 = level3_2
                    .Select((origStartTime, e) => origStartTime);
            */

            
            // To print any streamable
            streamA
                .ToStreamEventObservable()                      // Convert back to Observable (of StreamEvents)
                .Where(e => e.IsData)                           // Only pick data events from the stream
                .ForEach(e =>
                {
                    Console.WriteLine(e);
                })        // Print the events to the console
                ;
        }
    }
}