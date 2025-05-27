using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Driver;
using NumberLandStructure.Data;
using NumberLandStructure.Input;
using NumberLandStructure.Repository;
using NumberLandStructure.Reports;
using NumberLandStructure.Logic;
using System.Linq;

namespace NumberLandStructure
{
    class Program
    {
        private static ChildProgressRepository _repository;
        private static ReportGenerator _reportGenerator;

        static async Task Main(string[] args)
        {
            // MongoDB connection setup
            var connectionString = "mongodb://localhost:27017"; // Replace with your MongoDB connection string
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase("NumberLandDB"); // Replace with your database name



            // Initialize components
            _repository = new ChildProgressRepository(database);
            _reportGenerator = new ReportGenerator();

            Console.WriteLine("🎮 Welcome to NumberLand Progress Tracking System!");
            Console.WriteLine("==================================================\n");

            //try
            //{
            //    // Example usage scenarios
            //    await RunDemoScenarios();
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine($"❌ Error: {ex.Message}");
            //}

            string userId = "student_demo_001";

            try
            {
                // Demo the new tracking features
                await DemoItemCompletionTracking(_repository, userId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }


            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        #region Second Version
        static async Task DemoItemCompletionTracking(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("📊 ITEM COMPLETION TRACKING DEMO");
            Console.WriteLine("=================================\n");

            // Step 1: Add some sample activities
            await AddSampleActivities(repository, userId);

            // Step 2: Show which items have been completed
            await ShowCompletedItems(repository, userId);

            // Step 3: Show items in progress
            await ShowInProgressItems(repository, userId);

            // Step 4: Show items not started
            await ShowNotStartedItems(repository, userId);

            // Step 5: Show detailed category view
            await ShowCategoryDetails(repository, userId);

            // Step 6: Show completion summary
            await ShowCompletionSummary(repository, userId);

            // Step 7: Show items needing attention
            await ShowItemsNeedingAttention(repository, userId);
        }

        static async Task AddSampleActivities(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("📝 Adding sample activities to demonstrate tracking...\n");

            // Numbers 1-3: Completed with good scores
            for (int i = 1; i <= 3; i++)
            {
                // Add tracing (completed)
                var tracingInput = InputBuilder.CreateTracingInput(
                    ECDGameActivityName.Numbers, i.ToString(), true, 3, 40.0 + i * 5);
                await repository.AddTracingActivityAsync(userId, tracingInput);

                // Add quiz (passed)
                var quizInput = InputBuilder.CreateQuizInput(
                    ECDGameActivityName.Numbers, i.ToString(),
                    InputBuilder.CreateQuizDetail(QuizTypeDetail.Counting, true, 3, 2, false, 25.0, 60.0, 85 + i * 3)
                );
                await repository.AddQuizActivityAsync(userId, quizInput);

                Console.WriteLine($"✅ Number {i}: Completed with high score");
            }

            // Numbers 4-5: In progress (attempted but not completed)
            for (int i = 4; i <= 5; i++)
            {
                // Add tracing (not completed)
                var tracingInput = InputBuilder.CreateTracingInput(
                    ECDGameActivityName.Numbers, i.ToString(), false, 1, 60.0 + i * 5);
                await repository.AddTracingActivityAsync(userId, tracingInput);

                // Add quiz (failed)
                var quizInput = InputBuilder.CreateQuizInput(
                    ECDGameActivityName.Numbers, i.ToString(),
                    InputBuilder.CreateQuizDetail(QuizTypeDetail.Counting, false, 3, 0, false, 35.0, 60.0, 45)
                );
                await repository.AddQuizActivityAsync(userId, quizInput);

                Console.WriteLine($"🔄 Number {i}: In progress (needs more practice)");
            }

            // Letters A-C: Mixed results
            var letters = new[] { ("A", true, 3, 90), ("B", true, 2, 80), ("C", false, 1, 55) };
            foreach (var (letter, completed, stars, score) in letters)
            {
                var tracingInput = InputBuilder.CreateTracingInput(
                    ECDGameActivityName.CapitalAlphabet, letter, completed, stars, 35.0);
                await repository.AddTracingActivityAsync(userId, tracingInput);

                var quizInput = InputBuilder.CreateQuizInput(
                    ECDGameActivityName.CapitalAlphabet, letter,
                    InputBuilder.CreateQuizDetail(QuizTypeDetail.ObjectRecognition, completed, 3, completed ? 2 : 0, false, 20.0, 45.0, score)
                );
                await repository.AddQuizActivityAsync(userId, quizInput);

                Console.WriteLine($"{(completed ? "✅" : "🔄")} Letter {letter}: {(completed ? "Completed" : "In progress")} - {score}%");
            }

            Console.WriteLine("\n✅ Sample data added!\n");
        }

        static async Task ShowCompletedItems(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("✅ COMPLETED ITEMS");
            Console.WriteLine("==================");

            var completedItems = await repository.GetCompletedItemsAsync(userId);

            if (completedItems.Any())
            {
                Console.WriteLine($"🎉 Great job! {completedItems.Count} items completed:\n");

                foreach (var item in completedItems)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    Console.WriteLine($"{emoji} {item.Category} '{item.ItemName}':");
                    Console.WriteLine($"   📊 Score: {item.OverallScore:F0}%");
                    Console.WriteLine($"   🎯 Attempts: {item.TotalAttempts}");
                    Console.WriteLine($"   ⏱️ Time: {TimeSpan.FromSeconds(item.TotalTimeSpent):mm\\:ss}");
                    Console.WriteLine($"   📅 Completed: {item.CompletedDate:MMM dd, HH:mm}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No items completed yet. Keep practicing! 💪\n");
            }
        }

        static async Task ShowInProgressItems(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("🔄 ITEMS IN PROGRESS");
            Console.WriteLine("====================");

            var inProgressItems = await repository.GetInProgressItemsAsync(userId);

            if (inProgressItems.Any())
            {
                Console.WriteLine($"📚 {inProgressItems.Count} items being worked on:\n");

                foreach (var item in inProgressItems)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    Console.WriteLine($"{emoji} {item.Category} '{item.ItemName}':");
                    Console.WriteLine($"   📊 Current Score: {item.CurrentScore:F0}%");
                    Console.WriteLine($"   🎯 Attempts: {item.TotalAttempts}");
                    Console.WriteLine($"   ⚠️ Weakest Activity: {item.WeakestActivity}");
                    Console.WriteLine($"   📈 Completion Rate: {item.CompletionRate:F0}%");
                    Console.WriteLine($"   📅 Last Attempted: {item.LastAttempted:MMM dd, HH:mm}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("No items in progress.\n");
            }
        }

        static async Task ShowNotStartedItems(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("⭕ ITEMS NOT STARTED");
            Console.WriteLine("===================");

            var notStartedItems = await repository.GetNotStartedItemsAsync(userId);

            if (notStartedItems.Any())
            {
                Console.WriteLine($"🆕 {notStartedItems.Count} items ready to try:\n");

                // Group by category
                var groupedByCategory = notStartedItems.GroupBy(item => item.Category);

                foreach (var group in groupedByCategory)
                {
                    var emoji = GetCategoryEmoji(group.Key);
                    var items = group.Take(5).Select(i => i.ItemName); // Show first 5
                    var remaining = Math.Max(0, group.Count() - 5);

                    Console.WriteLine($"{emoji} {group.Key}: {string.Join(", ", items)}{(remaining > 0 ? $" (+{remaining} more)" : "")}");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("All available items have been attempted! 🎉\n");
            }
        }

        static async Task ShowCategoryDetails(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("📚 NUMBERS CATEGORY DETAILS");
            Console.WriteLine("============================");

            var categoryDetails = await repository.GetCategoryProgressAsync(userId, ECDGameActivityName.Numbers);

            Console.WriteLine($"📊 Numbers (1-10) Progress:\n");
            Console.WriteLine("Item | Status      | Score | Attempts | Time   | Activities");
            Console.WriteLine(new string('-', 60));

            foreach (var item in categoryDetails.ItemDetails.Take(10)) // Show first 10
            {
                var status = item.IsCompleted ? "✅ Done    " :
                            item.IsAttempted ? "🔄 Progress" :
                            "⭕ Not Started";

                var score = item.IsAttempted ? $"{item.OverallScore:F0}%" : "--";
                var attempts = item.IsAttempted ? item.TotalAttempts.ToString() : "--";
                var time = item.IsAttempted ? TimeSpan.FromSeconds(item.TotalTimeSpent).ToString(@"mm\:ss") : "--";

                // Count completed activities
                var completedActivities = item.ActivityStatus.Count(a => a.Value.IsCompleted);
                var totalActivities = item.ActivityStatus.Count(a => a.Value.IsAttempted);
                var activitiesInfo = item.IsAttempted ? $"{completedActivities}/{totalActivities}" : "--";

                Console.WriteLine($"{item.ItemName,4} | {status} | {score,5} | {attempts,8} | {time,6} | {activitiesInfo}");
            }
            Console.WriteLine();
        }

        static async Task ShowCompletionSummary(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("🎯 COMPLETION SUMMARY");
            Console.WriteLine("=====================");

            var completionSummary = await repository.GetCompletionSummaryAsync(userId);

            Console.WriteLine($"📊 Overall Progress Overview:\n");

            foreach (var category in completionSummary.CategoryCompletions)
            {
                var completion = category.Value;
                var emoji = GetCategoryEmoji(category.Key);

                Console.WriteLine($"{emoji} {category.Key}:");
                Console.WriteLine($"   📈 Completion: {completion.CompletionPercentage:F1}% ({completion.CompletedItems}/{completion.TotalItems})");
                Console.WriteLine($"   🎯 Attempted: {completion.AttemptPercentage:F1}% ({completion.AttemptedItems}/{completion.TotalItems})");

                // Simple progress bar
                var progressBar = GenerateProgressBar(completion.CompletionPercentage, 15);
                Console.WriteLine($"   {progressBar}");
                Console.WriteLine();
            }
        }

        static async Task ShowItemsNeedingAttention(ChildProgressRepository repository, string userId)
        {
            Console.WriteLine("⚠️ ITEMS NEEDING ATTENTION");
            Console.WriteLine("===========================");

            var attentionItems = await repository.GetItemsNeedingAttentionAsync(userId);

            if (attentionItems.Any())
            {
                Console.WriteLine($"🚨 {attentionItems.Count} items need extra practice:\n");

                foreach (var item in attentionItems.Take(5)) // Show top 5
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    var priority = GetPriorityIcon(item.PriorityLevel);

                    Console.WriteLine($"{priority} {emoji} {item.Category} '{item.ItemName}':");
                    Console.WriteLine($"   📊 Score: {item.CurrentScore:F0}% after {item.TotalAttempts} attempts");
                    Console.WriteLine($"   ⚠️ Issues: {string.Join(", ", item.AttentionReasons)}");
                    Console.WriteLine($"   💡 Recommendation: {item.RecommendedAction}");
                    Console.WriteLine();
                }
            }
            else
            {
                Console.WriteLine("🎉 Great job! No items need immediate attention.\n");
            }
        }

        #region Helper Methods

        static string GetCategoryEmoji(ECDGameActivityName category)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers: return "🔢";
                case ECDGameActivityName.CapitalAlphabet: return "🔤";
                case ECDGameActivityName.SmallAlphabet: return "🔡";
                case ECDGameActivityName.Shapes: return "🔷";
                case ECDGameActivityName.Colors: return "🎨";
                default: return "📝";
            }
        }

        static string GetPriorityIcon(int priorityLevel)
        {
            switch (priorityLevel)
            {
                case 5: return "🚨"; // Critical
                case 4: return "⚠️"; // High
                case 3: return "🔶"; // Medium
                case 2: return "🔸"; // Low
                default: return "ℹ️"; // Info
            }
        }

        static string GenerateProgressBar(double percentage, int width)
        {
            var completed = (int)Math.Round(percentage / 100.0 * width);
            var remaining = width - completed;
            return $"[{new string('█', completed)}{new string('░', remaining)}] {percentage:F0}%";
        }

        #endregion
        #endregion

        #region First Version

        static async Task RunDemoScenarios()
        {
            string childUserId = "child_demo_123";

            Console.WriteLine("🧪 Running Demo Scenarios...\n");

            // Scenario 1: Record tracing activities
            await Demo1_RecordTracingActivities(childUserId);

            // Scenario 2: Record quiz activities  
            await Demo2_RecordQuizActivities(childUserId);

            // Scenario 3: Generate reports
            await Demo3_GenerateReports(childUserId);

            // Scenario 4: Batch operations
            await Demo4_BatchOperations(childUserId);

            // Scenario 5: Learning recommendations
            await Demo5_LearningRecommendations(childUserId);
        }

        #region Demo Scenarios

        static async Task Demo1_RecordTracingActivities(string userId)
        {
            Console.WriteLine("📝 Demo 1: Recording Tracing Activities");
            Console.WriteLine("========================================");

            // Create tracing activities using InputBuilder
            var tracingActivities = new[]
            {
                InputBuilder.CreateTracingInput(ECDGameActivityName.Numbers, "1", true, 3, 42.5),
                InputBuilder.CreateTracingInput(ECDGameActivityName.Numbers, "2", true, 2, 55.2),
                InputBuilder.CreateTracingInput(ECDGameActivityName.Numbers, "3", false, 1, 68.8),
                InputBuilder.CreateTracingInput(ECDGameActivityName.CapitalAlphabet, "A", true, 3, 38.1),
                InputBuilder.CreateTracingInput(ECDGameActivityName.CapitalAlphabet, "B", true, 2, 45.6),
                InputBuilder.CreateTracingInput(ECDGameActivityName.Shapes, "Circle", true, 3, 33.4)
            };

            foreach (var activity in tracingActivities)
            {
                var result = await _repository.AddTracingActivityAsync(userId, activity);

                if (result.IsSuccess)
                {
                    Console.WriteLine($"✅ {activity.ActivityType} '{activity.ItemDetails}': {activity.StarsAchieved}/3 stars, {activity.TotalTime}s");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to add {activity.ItemDetails}: {result.Message}");
                }
            }

            Console.WriteLine();
        }

        static async Task Demo2_RecordQuizActivities(string userId)
        {
            Console.WriteLine("🎯 Demo 2: Recording Quiz Activities");
            Console.WriteLine("=====================================");

            // Create quiz activities with multiple quiz types
            var quiz1 = InputBuilder.CreateQuizInput(
                ECDGameActivityName.Numbers, "1",
                InputBuilder.CreateQuizDetail(QuizTypeDetail.Counting, true, 3, 2, false, 25.3, 60.0, 85),
                InputBuilder.CreateQuizDetail(QuizTypeDetail.Listening, true, 3, 1, false, 18.7, 30.0, 78),
                InputBuilder.CreateQuizDetail(QuizTypeDetail.TextToFigure, false, 3, 0, false, 45.0, 60.0, 35)
            );

            var quiz2 = InputBuilder.CreateQuizInput(
                ECDGameActivityName.CapitalAlphabet, "A",
                InputBuilder.CreateQuizDetail(QuizTypeDetail.ObjectRecognition, true, 3, 3, false, 12.5, 30.0, 95),
                InputBuilder.CreateQuizDetail(QuizTypeDetail.FiguresToText, true, 3, 2, false, 22.1, 45.0, 88)
            );

            var quizActivities = new[] { quiz1, quiz2 };

            foreach (var quizActivity in quizActivities)
            {
                var result = await _repository.AddQuizActivityAsync(userId, quizActivity);

                if (result.IsSuccess)
                {
                    var performance = quizActivity.GetPerformanceSummary();
                    Console.WriteLine($"✅ {quizActivity.ActivityType} '{quizActivity.ItemDetails}': " +
                        $"{performance.PassedQuizzes}/{performance.TotalQuizzes} passed, " +
                        $"Avg Score: {performance.AverageScore:F0}%");
                }
                else
                {
                    Console.WriteLine($"❌ Failed to add quiz for {quizActivity.ItemDetails}: {result.Message}");
                }
            }

            Console.WriteLine();
        }

        static async Task Demo3_GenerateReports(string userId)
        {
            Console.WriteLine("📊 Demo 3: Generating Reports");
            Console.WriteLine("==============================");

            try
            {
                // Get current progress
                var progress = await _repository.GetProgressAsync(userId, ProgressEventType.Daily);
                if (progress == null)
                {
                    Console.WriteLine("⚠️ No progress data found for today.");
                    return;
                }

                // Get weakness analysis
                var weaknessAnalysis = await _repository.GetWeaknessAnalysisAsync(userId, 7);

                // Generate comprehensive parent report
                var parentReport = _reportGenerator.GenerateParentReport(progress, weaknessAnalysis);
                var formattedReport = _reportGenerator.GenerateFormattedParentReport(parentReport);

                Console.WriteLine("📋 PARENT REPORT:");
                Console.WriteLine(formattedReport);

                // Generate daily summary
                var dailySummary = _reportGenerator.GenerateDailySummary(progress);
                Console.WriteLine("\n📅 DAILY SUMMARY:");
                Console.WriteLine(dailySummary);

                // Generate weakness summary
                var weaknessSummary = _reportGenerator.GenerateWeaknessSummary(weaknessAnalysis);
                Console.WriteLine("\n🔍 WEAKNESS ANALYSIS:");
                Console.WriteLine(weaknessSummary);

                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating reports: {ex.Message}");
            }
        }

        static async Task Demo4_BatchOperations(string userId)
        {
            Console.WriteLine("📦 Demo 4: Batch Operations");
            Console.WriteLine("============================");

            // Create batch input with multiple activities
            var batchInput = new BatchActivityInput
            {
                UserId = userId,
                TracingActivities = new List<TracingActivityInput>
                {
                    InputBuilder.CreateTracingInput(ECDGameActivityName.Numbers, "4", true, 2, 52.3),
                    InputBuilder.CreateTracingInput(ECDGameActivityName.Numbers, "5", true, 3, 41.7),
                    InputBuilder.CreateTracingInput(ECDGameActivityName.SmallAlphabet, "a", true, 2, 48.9)
                },
                QuizActivities = new List<QuizActivityInput>
                {
                    InputBuilder.CreateQuizInput(
                        ECDGameActivityName.Numbers, "4",
                        InputBuilder.CreateQuizDetail(QuizTypeDetail.Counting, true, 3, 2, false, 28.5, 60.0, 82)
                    ),
                    InputBuilder.CreateQuizInput(
                        ECDGameActivityName.SmallAlphabet, "a",
                        InputBuilder.CreateQuizDetail(QuizTypeDetail.ObjectRecognition, true, 3, 3, false, 15.2, 30.0, 91)
                    )
                }
            };

            var result = await _repository.AddBatchActivitiesAsync(batchInput);

            if (result.IsSuccess)
            {
                Console.WriteLine($"✅ {result.Message}");
                Console.WriteLine($"   📝 Tracing activities: {batchInput.TracingActivities.Count}");
                Console.WriteLine($"   🎯 Quiz activities: {batchInput.QuizActivities.Count}");
            }
            else
            {
                Console.WriteLine($"❌ Batch operation failed: {result.Message}");
            }

            Console.WriteLine();
        }

        static async Task Demo5_LearningRecommendations(string userId)
        {
            Console.WriteLine("🎯 Demo 5: Learning Recommendations");
            Console.WriteLine("====================================");

            try
            {
                // Get learning plan
                var learningPlan = await _repository.GetFocusedLearningPlanAsync(userId, 7, 7);
                var planSummary = _reportGenerator.GenerateLearningPlanSummary(learningPlan);

                Console.WriteLine("📅 LEARNING PLAN:");
                Console.WriteLine(planSummary);

                // Get activity recommendations
                var recommendations = await _repository.GetActivityRecommendationsAsync(userId, 7);

                if (recommendations.Any())
                {
                    Console.WriteLine("\n💡 ACTIVITY RECOMMENDATIONS:");
                    Console.WriteLine("==============================");

                    foreach (var rec in recommendations.Take(3))
                    {
                        Console.WriteLine($"🎮 {rec.ActivityType}:");
                        Console.WriteLine($"   📝 Reason: {rec.Reason}");
                        Console.WriteLine($"   🎯 Priority: {rec.Priority}");
                        Console.WriteLine($"   📋 Suggested Items: {string.Join(", ", rec.SuggestedItems.Take(3))}");
                        Console.WriteLine();
                    }
                }

                // Get detailed weakness analysis
                var weaknessDetails = await _repository.GetActivityWeaknessDetailsAsync(userId, 7);
                var weaknessReport = _reportGenerator.GenerateActivityWeaknessReport(weaknessDetails);

                Console.WriteLine("📊 DETAILED WEAKNESS ANALYSIS:");
                Console.WriteLine(weaknessReport);

                // Get overall statistics
                var statistics = await _repository.GetChildStatisticsAsync(userId, 30);
                var statsReport = _reportGenerator.GenerateStatisticsSummary(statistics);

                Console.WriteLine("\n📈 STATISTICS:");
                Console.WriteLine(statsReport);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error generating recommendations: {ex.Message}");
            }
        }

        #endregion

        #region Interactive Demo Methods

        static async Task RunInteractiveDemo()
        {
            var userId = "interactive_user";

            while (true)
            {
                Console.WriteLine("\n🎮 NumberLand Progress Tracking - Interactive Demo");
                Console.WriteLine("==================================================");
                Console.WriteLine("1. Add Tracing Activity");
                Console.WriteLine("2. Add Quiz Activity");
                Console.WriteLine("3. View Daily Summary");
                Console.WriteLine("4. View Learning Plan");
                Console.WriteLine("5. View Statistics");
                Console.WriteLine("6. Export Reports");
                Console.WriteLine("0. Exit");
                Console.WriteLine();
                Console.Write("Choose an option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await AddTracingActivityInteractive(userId);
                        break;
                    case "2":
                        await AddQuizActivityInteractive(userId);
                        break;
                    case "3":
                        await ShowDailySummary(userId);
                        break;
                    case "4":
                        await ShowLearningPlan(userId);
                        break;
                    case "5":
                        await ShowStatistics(userId);
                        break;
                    case "6":
                        await ExportReports(userId);
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("❌ Invalid option. Please try again.");
                        break;
                }
            }
        }

        static async Task AddTracingActivityInteractive(string userId)
        {
            Console.WriteLine("\n📝 Add Tracing Activity");
            Console.WriteLine("========================");

            // Get activity type
            Console.WriteLine("Activity Types:");
            Console.WriteLine("1. Numbers");
            Console.WriteLine("2. Capital Alphabet");
            Console.WriteLine("3. Small Alphabet");
            Console.WriteLine("4. Shapes");
            Console.WriteLine("5. Colors");
            Console.Write("Choose activity type (1-5): ");

            var activityTypeChoice = Console.ReadLine();
            ECDGameActivityName activityType;

            switch (activityTypeChoice)
            {
                case "1": activityType = ECDGameActivityName.Numbers; break;
                case "2": activityType = ECDGameActivityName.CapitalAlphabet; break;
                case "3": activityType = ECDGameActivityName.SmallAlphabet; break;
                case "4": activityType = ECDGameActivityName.Shapes; break;
                case "5": activityType = ECDGameActivityName.Colors; break;
                default:
                    Console.WriteLine("❌ Invalid choice.");
                    return;
            }

            Console.Write("Item details (e.g., '1', 'A', 'Circle'): ");
            var itemDetails = Console.ReadLine();

            Console.Write("Completed? (y/n): ");
            var completed = Console.ReadLine()?.ToLower() == "y";

            Console.Write("Stars achieved (0-3): ");
            if (!int.TryParse(Console.ReadLine(), out int stars) || stars < 0 || stars > 3)
            {
                Console.WriteLine("❌ Invalid stars value.");
                return;
            }

            Console.Write("Time taken (seconds): ");
            if (!double.TryParse(Console.ReadLine(), out double time) || time < 0)
            {
                Console.WriteLine("❌ Invalid time value.");
                return;
            }

            var tracingInput = InputBuilder.CreateTracingInput(activityType, itemDetails, completed, stars, time);
            var result = await _repository.AddTracingActivityAsync(userId, tracingInput);

            if (result.IsSuccess)
            {
                Console.WriteLine($"✅ Tracing activity added successfully!");
            }
            else
            {
                Console.WriteLine($"❌ Failed to add activity: {result.Message}");
            }
        }

        static async Task AddQuizActivityInteractive(string userId)
        {
            Console.WriteLine("\n🎯 Add Quiz Activity");
            Console.WriteLine("=====================");

            // Similar implementation for interactive quiz input
            // This would follow the same pattern as tracing activity
            Console.WriteLine("🚧 Quiz activity input - implementation similar to tracing");
            Console.WriteLine("   This would allow users to input quiz details interactively");
        }

        static async Task ShowDailySummary(string userId)
        {
            var progress = await _repository.GetProgressAsync(userId, ProgressEventType.Daily);
            if (progress != null)
            {
                var summary = _reportGenerator.GenerateDailySummary(progress);
                Console.WriteLine(summary);
            }
            else
            {
                Console.WriteLine("⚠️ No progress data found for today.");
            }
        }

        static async Task ShowLearningPlan(string userId)
        {
            var plan = await _repository.GetFocusedLearningPlanAsync(userId);
            var summary = _reportGenerator.GenerateLearningPlanSummary(plan);
            Console.WriteLine(summary);
        }

        static async Task ShowStatistics(string userId)
        {
            var stats = await _repository.GetChildStatisticsAsync(userId);
            var summary = _reportGenerator.GenerateStatisticsSummary(stats);
            Console.WriteLine(summary);
        }

        static async Task ExportReports(string userId)
        {
            Console.WriteLine("\n📤 Export Reports");
            Console.WriteLine("==================");

            try
            {
                var progress = await _repository.GetProgressAsync(userId, ProgressEventType.Daily);
                if (progress == null)
                {
                    Console.WriteLine("⚠️ No progress data to export.");
                    return;
                }

                var weaknessAnalysis = await _repository.GetWeaknessAnalysisAsync(userId);
                var report = _reportGenerator.GenerateParentReport(progress, weaknessAnalysis);
                var learningPlan = await _repository.GetFocusedLearningPlanAsync(userId);

                // Export to CSV
                var progressCSV = ReportExporter.ExportProgressToCSV(report);
                var activityCSV = ReportExporter.ExportActivityPerformanceToCSV(report);
                var planCSV = ReportExporter.ExportLearningPlanToCSV(learningPlan);

                Console.WriteLine("📊 Progress CSV:");
                Console.WriteLine(progressCSV);
                Console.WriteLine("\n🎮 Activity Performance CSV:");
                Console.WriteLine(activityCSV);
                Console.WriteLine("\n📅 Learning Plan CSV:");
                Console.WriteLine(planCSV);

                Console.WriteLine("✅ Reports exported successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Export failed: {ex.Message}");
            }
        }

        #endregion
    }
    #endregion
}