using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NumberLandStructure.Data;
using NumberLandStructure.Repository;
using NumberLandStructure.Tracking;

namespace NumberLandStructure.Viewer
{
    /// <summary>
    /// Provides formatted views and displays for progress tracking data
    /// </summary>
    public class ProgressTrackingViewer
    {
        /// <summary>
        /// Generate a comprehensive progress dashboard
        /// </summary>
        public string GenerateProgressDashboard(ProgressOverview overview)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📊 PROGRESS DASHBOARD - {overview.UserId}");
            sb.AppendLine($"Generated: {overview.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            foreach (var category in overview.CategoryProgress.OrderBy(c => c.Key))
            {
                var tracking = category.Value;
                var emoji = GetCategoryEmoji(category.Key);

                sb.AppendLine($"{emoji} {category.Key.ToString().ToUpper()}");
                sb.AppendLine(new string('-', 30));
                sb.AppendLine($"📈 Progress: {tracking.CompletedCount}/{tracking.TotalExpected} ({tracking.CompletionRate:F1}%)");
                sb.AppendLine($"🎯 Attempted: {tracking.AttemptedCount}");

                if (tracking.CompletedItems.Any())
                {
                    sb.AppendLine($"✅ Completed: {string.Join(", ", tracking.CompletedItems.OrderBy(x => x))}");
                }

                if (tracking.InProgressItems.Any())
                {
                    sb.AppendLine($"🔄 In Progress: {string.Join(", ", tracking.InProgressItems.OrderBy(x => x))}");
                }

                if (tracking.NotStartedItems.Any())
                {
                    var notStartedPreview = tracking.NotStartedItems.Take(5);
                    var moreCount = tracking.NotStartedItems.Count - 5;
                    sb.Append($"⭕ Not Started: {string.Join(", ", notStartedPreview.OrderBy(x => x))}");
                    if (moreCount > 0)
                        sb.Append($" (+{moreCount} more)");
                    sb.AppendLine();
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate detailed category view
        /// </summary>
        public string GenerateCategoryView(CategoryDetailView categoryView)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📚 {categoryView.Category.ToString().ToUpper()} - DETAILED VIEW");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            // Summary stats
            var attempted = categoryView.ItemDetails.Count(i => i.IsAttempted);
            var completed = categoryView.ItemDetails.Count(i => i.IsCompleted);
            var notStarted = categoryView.ItemDetails.Count(i => !i.IsAttempted);

            sb.AppendLine($"📊 SUMMARY:");
            sb.AppendLine($"   Total Items: {categoryView.ExpectedItems.Count}");
            sb.AppendLine($"   ✅ Completed: {completed}");
            sb.AppendLine($"   🔄 In Progress: {attempted - completed}");
            sb.AppendLine($"   ⭕ Not Started: {notStarted}");
            sb.AppendLine($"   📈 Completion Rate: {(double)completed / categoryView.ExpectedItems.Count * 100:F1}%");
            sb.AppendLine();

            // Detailed item list
            sb.AppendLine($"📋 ITEM DETAILS:");
            sb.AppendLine("Item      | Status    | Score | Attempts | Time   | Weakest Activity");
            sb.AppendLine(new string('-', 75));

            foreach (var item in categoryView.ItemDetails.OrderBy(GetItemOrder))
            {
                sb.Append($"{item.ItemName,-9} | ");

                if (item.IsCompleted)
                    sb.Append("✅ Done   | ");
                else if (item.IsAttempted)
                    sb.Append("🔄 Prog   | ");
                else
                    sb.Append("⭕ None   | ");

                if (item.IsAttempted)
                {
                    sb.Append($"{item.OverallScore,5:F0}% | ");
                    sb.Append($"{item.TotalAttempts,8} | ");
                    sb.Append($"{TimeSpan.FromSeconds(item.TotalTimeSpent),6:mm\\:ss} | ");

                    var weakestActivity = item.ActivityStatus
                        .Where(a => a.Value.IsAttempted)
                        .OrderBy(a => a.Value.SuccessRate)
                        .FirstOrDefault();

                    if (weakestActivity.Key != null)
                        sb.Append(weakestActivity.Key);
                    else
                        sb.Append("None");
                }
                else
                {
                    sb.Append("   -- |       -- |    -- | Not started");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate completion summary report
        /// </summary>
        public string GenerateCompletionSummary(CompletionSummary summary)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🎯 COMPLETION SUMMARY - {summary.UserId}");
            sb.AppendLine($"Generated: {summary.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            foreach (var category in summary.CategoryCompletions.OrderBy(c => c.Value.CompletionPercentage))
            {
                var completion = category.Value;
                var emoji = GetCategoryEmoji(category.Key);

                sb.AppendLine($"{emoji} {category.Key}:");
                sb.AppendLine($"   📊 Completion: {completion.CompletionPercentage:F1}% ({completion.CompletedItems}/{completion.TotalItems})");
                sb.AppendLine($"   🎯 Attempted: {completion.AttemptPercentage:F1}% ({completion.AttemptedItems}/{completion.TotalItems})");

                // Progress bar
                var progressBar = GenerateProgressBar(completion.CompletionPercentage, 20);
                sb.AppendLine($"   {progressBar}");

                if (completion.CompletedItemsList.Any())
                {
                    sb.AppendLine($"   ✅ Completed: {string.Join(", ", completion.CompletedItemsList.Take(10))}");
                    if (completion.CompletedItemsList.Count > 10)
                        sb.AppendLine($"      ... and {completion.CompletedItemsList.Count - 10} more");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate activity-specific progress view
        /// </summary>
        public string GenerateActivityProgressView(ActivityProgressView activityView)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"🎮 {activityView.ActivityType.ToUpper()} ACTIVITY PROGRESS");
            sb.AppendLine($"Generated: {activityView.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            foreach (var category in activityView.CategoryResults.Where(cr => cr.Value.Any()))
            {
                var emoji = GetCategoryEmoji(category.Key);
                sb.AppendLine($"{emoji} {category.Key}:");
                sb.AppendLine("Item    | Attempts | Success | Rate  | Avg Time | Status");
                sb.AppendLine(new string('-', 55));

                foreach (var result in category.Value.OrderBy(r => r.ItemName))
                {
                    sb.Append($"{result.ItemName,-7} | ");
                    sb.Append($"{result.Attempts,8} | ");
                    sb.Append($"{result.Successes,7} | ");
                    sb.Append($"{result.SuccessRate,5:F0}% | ");
                    sb.Append($"{result.AverageTime,8:F1}s | ");

                    if (result.SuccessRate >= 80)
                        sb.AppendLine("✅ Good");
                    else if (result.SuccessRate >= 60)
                        sb.AppendLine("🔄 Fair");
                    else
                        sb.AppendLine("❌ Needs Work");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate simple completed items list
        /// </summary>
        public string GenerateCompletedItemsList(List<CompletedItem> completedItems)
        {
            if (!completedItems.Any())
                return "📝 No completed items yet.";

            var sb = new StringBuilder();

            sb.AppendLine($"✅ COMPLETED ITEMS ({completedItems.Count} total)");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine();

            var groupedByCategory = completedItems.GroupBy(item => item.Category);

            foreach (var group in groupedByCategory.OrderBy(g => g.Key))
            {
                var emoji = GetCategoryEmoji(group.Key);
                sb.AppendLine($"{emoji} {group.Key}:");

                foreach (var item in group.OrderByDescending(i => i.CompletedDate))
                {
                    sb.AppendLine($"   ✅ {item.ItemName} - Score: {item.OverallScore:F0}% ({item.CompletedDate:MMM dd})");
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate items needing attention report
        /// </summary>
        public string GenerateAttentionItemsReport(List<AttentionItem> attentionItems)
        {
            if (!attentionItems.Any())
                return "🎉 Great job! No items need immediate attention.";

            var sb = new StringBuilder();

            sb.AppendLine($"⚠️ ITEMS NEEDING ATTENTION ({attentionItems.Count} items)");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            foreach (var item in attentionItems.Take(10)) // Show top 10
            {
                var emoji = GetCategoryEmoji(item.Category);
                var priority = GetPriorityIndicator(item.PriorityLevel);

                sb.AppendLine($"{priority} {emoji} {item.Category} '{item.ItemName}'");
                sb.AppendLine($"   📊 Score: {item.CurrentScore:F0}% after {item.TotalAttempts} attempts");
                sb.AppendLine($"   📅 Last attempted: {item.LastAttempted:MMM dd}");
                sb.AppendLine($"   ⚠️ Issues: {string.Join(", ", item.AttentionReasons)}");
                sb.AppendLine($"   💡 Recommendation: {item.RecommendedAction}");
                sb.AppendLine();
            }

            if (attentionItems.Count > 10)
            {
                sb.AppendLine($"... and {attentionItems.Count - 10} more items");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate progress statistics summary
        /// </summary>
        public string GenerateProgressStatistics(ProgressStatistics stats)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📈 PROGRESS STATISTICS - {stats.UserId}");
            sb.AppendLine($"Period: {stats.PeriodStart:MMM dd} - {stats.PeriodEnd:MMM dd} ({stats.DaysAnalyzed} days)");
            sb.AppendLine($"Generated: {stats.GeneratedDate:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            sb.AppendLine("📊 OVERALL STATISTICS:");
            sb.AppendLine($"   📚 Items Attempted: {stats.TotalItemsAttempted}");
            sb.AppendLine($"   ✅ Items Completed: {stats.TotalItemsCompleted}");
            sb.AppendLine($"   📈 Completion Rate: {stats.CompletionRate:F1}%");
            sb.AppendLine($"   🎯 Total Attempts: {stats.TotalAttempts}");
            sb.AppendLine($"   ⏱️ Total Time: {TimeSpan.FromSeconds(stats.TotalTimeSpent):hh\\:mm\\:ss}");
            sb.AppendLine($"   📊 Average Score: {stats.AverageScore:F1}%");
            sb.AppendLine();

            sb.AppendLine("🎨 ACTIVITY BREAKDOWN:");
            sb.AppendLine($"   📝 Tracing:");
            sb.AppendLine($"      • Attempts: {stats.TracingAttempts}");
            sb.AppendLine($"      • Completions: {stats.TracingCompletions}");
            sb.AppendLine($"      • Success Rate: {stats.TracingSuccessRate:F1}%");
            sb.AppendLine();
            sb.AppendLine($"   🎯 Quizzes:");
            sb.AppendLine($"      • Attempts: {stats.QuizAttempts}");
            sb.AppendLine($"      • Failures: {stats.QuizFailures}");
            sb.AppendLine($"      • Success Rate: {stats.QuizSuccessRate:F1}%");
            sb.AppendLine();

            // Daily averages
            if (stats.DaysAnalyzed > 0)
            {
                sb.AppendLine("📅 DAILY AVERAGES:");
                sb.AppendLine($"   📚 Items per day: {(double)stats.TotalItemsAttempted / stats.DaysAnalyzed:F1}");
                sb.AppendLine($"   🎯 Attempts per day: {(double)stats.TotalAttempts / stats.DaysAnalyzed:F1}");
                sb.AppendLine($"   ⏱️ Time per day: {TimeSpan.FromSeconds(stats.TotalTimeSpent / stats.DaysAnalyzed):mm\\:ss}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate a quick status overview
        /// </summary>
        public string GenerateQuickStatus(string userId, CompletionSummary summary)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"⚡ QUICK STATUS - {userId}");
            sb.AppendLine(new string('-', 30));

            var totalItems = summary.CategoryCompletions.Sum(c => c.Value.TotalItems);
            var completedItems = summary.CategoryCompletions.Sum(c => c.Value.CompletedItems);
            var overallCompletion = totalItems > 0 ? (double)completedItems / totalItems * 100 : 0;

            sb.AppendLine($"📊 Overall Progress: {completedItems}/{totalItems} ({overallCompletion:F1}%)");

            var progressBar = GenerateProgressBar(overallCompletion, 15);
            sb.AppendLine($"   {progressBar}");
            sb.AppendLine();

            sb.AppendLine("📚 By Category:");
            foreach (var category in summary.CategoryCompletions.OrderByDescending(c => c.Value.CompletionPercentage))
            {
                var completion = category.Value;
                var emoji = GetCategoryEmoji(category.Key);
                var miniBar = GenerateProgressBar(completion.CompletionPercentage, 8);

                sb.AppendLine($"   {emoji} {category.Key}: {miniBar} {completion.CompletionPercentage:F0}%");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate simple progress grid for console display
        /// </summary>
        public string GenerateSimpleProgressGrid(ProgressOverview overview)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📊 PROGRESS GRID - {overview.UserId}");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine();

            sb.AppendLine("Category         | Progress | Completed Items");
            sb.AppendLine(new string('-', 50));

            foreach (var category in overview.CategoryProgress.OrderBy(c => c.Key))
            {
                var tracking = category.Value;
                var emoji = GetCategoryEmoji(category.Key);
                var categoryName = category.Key.ToString();

                sb.Append($"{emoji} {categoryName,-12} | ");

                var progressBar = GenerateProgressBar(tracking.CompletionRate, 8);
                sb.Append($"{progressBar} | ");

                if (tracking.CompletedItems.Any())
                {
                    var completedDisplay = string.Join(", ", tracking.CompletedItems.Take(5));
                    if (tracking.CompletedItems.Count > 5)
                        completedDisplay += $" (+{tracking.CompletedItems.Count - 5})";
                    sb.Append(completedDisplay);
                }
                else
                {
                    sb.Append("None yet");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate today's focus items
        /// </summary>
        public string GenerateTodaysFocus(List<InProgressItem> inProgressItems, List<NotStartedItem> notStartedItems)
        {
            var sb = new StringBuilder();

            sb.AppendLine("🎯 TODAY'S FOCUS");
            sb.AppendLine(new string('=', 20));
            sb.AppendLine();

            // Items that need improvement (lowest scores first)
            var needsImprovement = inProgressItems
                .Where(item => item.CurrentScore < 70)
                .OrderBy(item => item.CurrentScore)
                .Take(3)
                .ToList();

            if (needsImprovement.Any())
            {
                sb.AppendLine("🔧 ITEMS TO IMPROVE:");
                foreach (var item in needsImprovement)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    sb.AppendLine($"   {emoji} {item.Category} '{item.ItemName}' - {item.CurrentScore:F0}% (Focus on {item.WeakestActivity})");
                }
                sb.AppendLine();
            }

            // New items to try
            var newItems = notStartedItems.Take(3).ToList();
            if (newItems.Any())
            {
                sb.AppendLine("🆕 NEW ITEMS TO TRY:");
                foreach (var item in newItems)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    sb.AppendLine($"   {emoji} {item.Category} '{item.ItemName}'");
                }
                sb.AppendLine();
            }

            // Items close to completion
            var almostDone = inProgressItems
                .Where(item => item.CurrentScore >= 70 && item.CompletionRate >= 80)
                .OrderByDescending(item => item.CurrentScore)
                .Take(2)
                .ToList();

            if (almostDone.Any())
            {
                sb.AppendLine("🏁 ALMOST FINISHED:");
                foreach (var item in almostDone)
                {
                    var emoji = GetCategoryEmoji(item.Category);
                    sb.AppendLine($"   {emoji} {item.Category} '{item.ItemName}' - {item.CurrentScore:F0}%");
                }
            }

            return sb.ToString();
        }

        #region Helper Methods

        private string GetCategoryEmoji(ECDGameActivityName category)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers:
                    return "🔢";
                case ECDGameActivityName.CapitalAlphabet:
                    return "🔤";
                case ECDGameActivityName.SmallAlphabet:
                    return "🔡";
                case ECDGameActivityName.Shapes:
                    return "🔷";
                case ECDGameActivityName.Colors:
                    return "🎨";
                default:
                    return "📝";
            }
        }

        private string GenerateProgressBar(double percentage, int width)
        {
            var completed = (int)Math.Round(percentage / 100.0 * width);
            var remaining = width - completed;

            return $"[{'█'.ToString().PadRight(completed, '█')}{'░'.ToString().PadRight(remaining, '░')}] {percentage:F0}%";
        }

        private string GetPriorityIndicator(int priorityLevel)
        {
            switch (priorityLevel)
            {
                case 5:
                    return "🚨"; // Critical
                case 4:
                    return "⚠️"; // High
                case 3:
                    return "🔶"; // Medium
                case 2:
                    return "🔸"; // Low
                default:
                    return "ℹ️"; // Info
            }
        }

        private int GetItemOrder(ItemDetailView item)
        {
            // Try to parse as number first
            if (int.TryParse(item.ItemName, out int number))
                return number;

            // Then try as letter
            if (item.ItemName.Length == 1)
            {
                char c = item.ItemName[0];
                if (c >= 'A' && c <= 'Z')
                    return c - 'A' + 1;
                if (c >= 'a' && c <= 'z')
                    return c - 'a' + 1;
            }

            // Default alphabetical order
            return item.ItemName.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    /// Console-friendly progress display utilities
    /// </summary>
    public static class ConsoleProgressDisplay
    {
        /// <summary>
        /// Display progress with color coding (for console apps)
        /// </summary>
        public static void DisplayColoredProgress(CompletionSummary summary)
        {
            Console.WriteLine($"🎯 COMPLETION OVERVIEW - {summary.UserId}");
            Console.WriteLine(new string('=', 40));
            Console.WriteLine();

            foreach (var category in summary.CategoryCompletions.OrderBy(c => c.Key))
            {
                var completion = category.Value;
                var emoji = GetCategoryEmoji(category.Key);

                Console.Write($"{emoji} {category.Key}: ");

                // Color-code based on completion percentage
                if (completion.CompletionPercentage >= 80)
                    Console.ForegroundColor = ConsoleColor.Green;
                else if (completion.CompletionPercentage >= 60)
                    Console.ForegroundColor = ConsoleColor.Yellow;
                else if (completion.CompletionPercentage >= 40)
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                else
                    Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine($"{completion.CompletionPercentage:F1}% ({completion.CompletedItems}/{completion.TotalItems})");
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Display a live updating progress counter
        /// </summary>
        public static void DisplayLiveProgress(string activity, int current, int total)
        {
            var percentage = (double)current / total * 100;
            var progressBar = GenerateProgressBar(percentage, 20);

            Console.Write($"\r{activity}: {progressBar} {current}/{total}");

            if (current == total)
            {
                Console.WriteLine(" ✅ Complete!");
            }
        }

        private static string GetCategoryEmoji(ECDGameActivityName category)
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

        private static string GenerateProgressBar(double percentage, int width)
        {
            var completed = (int)Math.Round(percentage / 100.0 * width);
            var remaining = width - completed;
            return $"[{new string('█', completed)}{new string('░', remaining)}]";
        }
    }
}