using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NumberLandStructure
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // MongoDB connection setup
            var connectionString = "mongodb://localhost:27017"; // Replace with your MongoDB connection string
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("NumberLandDB"); // Replace with your database name

            // Initialize repository
            var repository = new ChildProgressRepository(database);

            // Example usage
            await RunExampleScenario(repository);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static async Task RunExampleScenario(ChildProgressRepository repository)
        {
            string childUserId = "child_123"; // Example child ID

            Console.WriteLine("=== NumberLand Progress Tracking Demo ===\n");

            // Example 1: Record tracing activity
            Console.WriteLine("1. Recording tracing activities...");
            await RecordTracingActivities(repository, childUserId);

            // Example 2: Record quiz activities
            Console.WriteLine("\n2. Recording quiz activities...");
            await RecordQuizActivities(repository, childUserId);

            // Example 3: Generate parent report
            Console.WriteLine("\n3. Generating parent report...");
            await GenerateParentReport(repository, childUserId);

            // Example 4: Get focused learning plan
            Console.WriteLine("\n4. Creating focused learning plan...");
            await GetLearningPlan(repository, childUserId);

            // Example 5: Analyze activity weaknesses
            Console.WriteLine("\n5. Analyzing activity weaknesses...");
            await AnalyzeWeaknesses(repository, childUserId);
        }

        static async Task RecordTracingActivities(ChildProgressRepository repository, string childUserId)
        {
            // Record Number 1 tracing activity
            var tracingInput1 = new TracingActivityInput
            {
                ActivityType = ECDGameActivityName.Numbers,
                ItemDetails = "1",
                Completed = true,
                StarsAchieved = 2,
                MaxStars = 3,
                TotalTime = 45.5 // seconds
            };
            await repository.UpdateProgressWithTracing(childUserId, tracingInput1);
            Console.WriteLine("✓ Recorded Number 1 tracing: 2/3 stars, 45.5 seconds");

            // Record Number 2 tracing activity
            var tracingInput2 = new TracingActivityInput
            {
                ActivityType = ECDGameActivityName.Numbers,
                ItemDetails = "2",
                Completed = false, // Child didn't complete it
                StarsAchieved = 1,
                MaxStars = 3,
                TotalTime = 65.2
            };
            await repository.UpdateProgressWithTracing(childUserId, tracingInput2);
            Console.WriteLine("✓ Recorded Number 2 tracing: 1/3 stars, 65.2 seconds (incomplete)");

            // Record Letter A tracing activity
            var tracingInputA = new TracingActivityInput
            {
                ActivityType = ECDGameActivityName.CapitalAlphabet,
                ItemDetails = "A",
                Completed = true,
                StarsAchieved = 3,
                MaxStars = 3,
                TotalTime = 38.1
            };
            await repository.UpdateProgressWithTracing(childUserId, tracingInputA);
            Console.WriteLine("✓ Recorded Letter A tracing: 3/3 stars, 38.1 seconds");
        }

        static async Task RecordQuizActivities(ChildProgressRepository repository, string childUserId)
        {
            // Quiz for Number 1 (multiple quiz types)
            var quizInput1 = new QuizActivityInput
            {
                ActivityType = ECDGameActivityName.Numbers,
                ItemDetails = "1",
                QuizDetails = new List<QuizDetail>
                {
                    new QuizDetail
                    {
                        Type = QuizTypeDetail.Counting,
                        Completed = true,
                        TotalLives = 3,
                        LivesRemaining = 2,
                        QuizTimeOut = false,
                        TimeTaken = 25.3,
                        TimeGiven = 60.0,
                        Score = 85
                    },
                    new QuizDetail
                    {
                        Type = QuizTypeDetail.Listening,
                        Completed = true,
                        TotalLives = 3,
                        LivesRemaining = 1,
                        QuizTimeOut = false,
                        TimeTaken = 18.7,
                        TimeGiven = 30.0,
                        Score = 75
                    },
                    new QuizDetail
                    {
                        Type = QuizTypeDetail.TextToFigure,
                        Completed = false,
                        TotalLives = 3,
                        LivesRemaining = 0, // Failed - no lives left
                        QuizTimeOut = false,
                        TimeTaken = 45.0,
                        TimeGiven = 60.0,
                        Score = 35
                    }
                }
            };
            await repository.UpdateProgressWithQuiz(childUserId, quizInput1);
            Console.WriteLine("✓ Recorded Number 1 quiz: Counting(85%), Listening(75%), Text-to-Figure(35% - Failed)");

            // Quiz for Letter A
            var quizInputA = new QuizActivityInput
            {
                ActivityType = ECDGameActivityName.CapitalAlphabet,
                ItemDetails = "A",
                QuizDetails = new List<QuizDetail>
                {
                    new QuizDetail
                    {
                        Type = QuizTypeDetail.ObjectRecognition,
                        Completed = true,
                        TotalLives = 3,
                        LivesRemaining = 3,
                        QuizTimeOut = false,
                        TimeTaken = 12.5,
                        TimeGiven = 30.0,
                        Score = 95
                    },
                    new QuizDetail
                    {
                        Type = QuizTypeDetail.FiguresToText,
                        Completed = true,
                        TotalLives = 3,
                        LivesRemaining = 2,
                        QuizTimeOut = false,
                        TimeTaken = 22.1,
                        TimeGiven = 45.0,
                        Score = 88
                    }
                }
            };
            await repository.UpdateProgressWithQuiz(childUserId, quizInputA);
            Console.WriteLine("✓ Recorded Letter A quiz: Object Recognition(95%), Figure-to-Text(88%)");
        }

        static async Task GenerateParentReport(ChildProgressRepository repository, string childUserId)
        {
            var report = await repository.GetParentReport(childUserId, ProgressEventType.Daily);

            Console.WriteLine("PARENT PROGRESS REPORT:");
            Console.WriteLine("========================");
            Console.WriteLine(report.GenerateReport());
        }

        static async Task GetLearningPlan(ChildProgressRepository repository, string childUserId)
        {
            var learningPlan = await repository.GetFocusedLearningPlan(childUserId, 7);

            Console.WriteLine("FOCUSED LEARNING PLAN:");
            Console.WriteLine("======================");
            Console.WriteLine(learningPlan.GetPlanSummary());
        }

        static async Task AnalyzeWeaknesses(ChildProgressRepository repository, string childUserId)
        {
            var weaknesses = await repository.GetActivityWeaknesses(childUserId, 7);

            Console.WriteLine("ACTIVITY WEAKNESS ANALYSIS:");
            Console.WriteLine("============================");

            foreach (var weakness in weaknesses)
            {
                Console.WriteLine($"\n{weakness.Key} Activity:");
                Console.WriteLine($"  Overall Success Rate: {weakness.Value.OverallSuccessRate:F1}%");
                Console.WriteLine($"  Total Attempts: {weakness.Value.TotalAttempts}");

                if (weakness.Value.TopWeakItems.Any())
                {
                    Console.WriteLine($"  Weakest Items: {string.Join(", ", weakness.Value.TopWeakItems)}");
                }
            }
        }

        // Example method to simulate a complete learning session
        static async Task SimulateCompleteLearningSession(ChildProgressRepository repository, string childUserId)
        {
            Console.WriteLine("\n=== SIMULATING COMPLETE LEARNING SESSION ===");

            // Simulate child learning numbers 1-3
            for (int number = 1; number <= 3; number++)
            {
                // Tracing activity
                var tracingInput = new TracingActivityInput
                {
                    ActivityType = ECDGameActivityName.Numbers,
                    ItemDetails = number.ToString(),
                    Completed = true,
                    StarsAchieved = new Random().Next(1, 4), // Random stars 1-3
                    MaxStars = 3,
                    TotalTime = new Random().Next(30, 90) // Random time 30-90 seconds
                };
                await repository.UpdateProgressWithTracing(childUserId, tracingInput);

                // Quiz activities
                var quizInput = new QuizActivityInput
                {
                    ActivityType = ECDGameActivityName.Numbers,
                    ItemDetails = number.ToString(),
                    QuizDetails = new List<QuizDetail>
                    {
                        new QuizDetail
                        {
                            Type = QuizTypeDetail.Counting,
                            Completed = true,
                            TotalLives = 3,
                            LivesRemaining = new Random().Next(1, 4),
                            QuizTimeOut = false,
                            TimeTaken = new Random().Next(15, 45),
                            TimeGiven = 60.0,
                            Score = new Random().Next(60, 100)
                        },
                        new QuizDetail
                        {
                            Type = QuizTypeDetail.Listening,
                            Completed = true,
                            TotalLives = 3,
                            LivesRemaining = new Random().Next(0, 4),
                            QuizTimeOut = false,
                            TimeTaken = new Random().Next(10, 30),
                            TimeGiven = 30.0,
                            Score = new Random().Next(50, 95)
                        }
                    }
                };
                await repository.UpdateProgressWithQuiz(childUserId, quizInput);

                Console.WriteLine($"✓ Completed learning session for Number {number}");
            }

            Console.WriteLine("Learning session simulation completed!");
        }
    }
}