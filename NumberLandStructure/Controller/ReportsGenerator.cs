using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NumberLandStructure.Data;
using NumberLandStructure.Logic;

namespace NumberLandStructure.Reports
{
    /// <summary>
    /// Generates various types of reports for parents and educators
    /// </summary>
    public class ReportGenerator
    {
        private readonly AnalysisLogic _analysisLogic;

        public ReportGenerator()
        {
            _analysisLogic = new AnalysisLogic();
        }

        /// <summary>
        /// Generate comprehensive parent progress report
        /// </summary>
        public ParentProgressReport GenerateParentReport(ChildProgress progress, WeaknessAnalysis weaknessAnalysis)
        {
            var report = new ParentProgressReport
            {
                ChildUserId = progress.UserId,
                ReportDate = DateTime.UtcNow,
                ReportPeriod = progress.EventType,
                CategoryProgress = new Dictionary<ECDGameActivityName, CategoryProgressSummary>(),
                ActivityPerformance = new Dictionary<string, ActivityPerformanceSummary>(),
                TopPriorityItems = new List<FocusItem>(),
                ActivityRecommendations = new List<ActivityRecommendation>()
            };

            // Calculate overall stats
            CalculateOverallStats(progress, report);

            // Calculate category progress
            CalculateCategoryProgress(progress, report, ECDGameActivityName.Numbers, progress.NumberProgressDict);
            CalculateCategoryProgress(progress, report, ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict);
            CalculateCategoryProgress(progress, report, ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict);
            CalculateCategoryProgress(progress, report, ECDGameActivityName.Shapes, progress.ShapeProgressDict);
            CalculateCategoryProgress(progress, report, ECDGameActivityName.Colors, progress.ColorProgressDict);

            // Calculate activity performance
            CalculateActivityPerformance(progress, report);

            // Set weakness analysis results
            report.TopPriorityItems = _analysisLogic.GetPrioritizedFocusItems(weaknessAnalysis);

            return report;
        }

        /// <summary>
        /// Generate formatted text report for parents
        /// </summary>
        public string GenerateFormattedParentReport(ParentProgressReport report)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📊 PROGRESS REPORT - {report.ReportDate:MMM dd, yyyy}");
            sb.AppendLine($"Period: {report.ReportPeriod}");
            sb.AppendLine(new string('=', 50));

            // Overall Summary
            sb.AppendLine("\n📈 OVERALL SUMMARY");
            sb.AppendLine($"Total Activities: {report.TotalActivitiesCompleted}");
            sb.AppendLine($"Total Time: {TimeSpan.FromSeconds(report.TotalTimeSpent):hh\\:mm\\:ss}");
            sb.AppendLine($"Success Rate: {report.OverallSuccessRate:F1}%");

            // Category Progress
            sb.AppendLine("\n📚 CATEGORY PROGRESS");
            foreach (var category in report.CategoryProgress.OrderBy(c => c.Value.CompletionRate))
            {
                var cat = category.Value;
                sb.AppendLine($"\n{GetCategoryEmoji(category.Key)} {category.Key}:");
                sb.AppendLine($"   • Completion: {cat.CompletionRate:F0}% ({cat.ItemsCompleted}/{cat.TotalItems})");
                sb.AppendLine($"   • Success Rate: {cat.SuccessRate:F0}%");
                sb.AppendLine($"   • Time Spent: {TimeSpan.FromSeconds(cat.TimeSpent):mm\\:ss}");

                if (cat.WeakItems.Any())
                {
                    sb.AppendLine($"   • Need Practice: {string.Join(", ", cat.WeakItems.Take(3))}");
                }
            }

            // Activity Performance
            sb.AppendLine("\n🎮 ACTIVITY PERFORMANCE");
            foreach (var activity in report.ActivityPerformance.OrderBy(a => a.Value.SuccessRate))
            {
                var perf = activity.Value;
                sb.AppendLine($"\n{activity.Key}:");
                sb.AppendLine($"   • Success Rate: {perf.SuccessRate:F0}%");
                sb.AppendLine($"   • Attempts: {perf.TotalAttempts}");
                sb.AppendLine($"   • Avg Time: {perf.AverageTime:F1}s");

                if (perf.NeedsImprovement)
                {
                    sb.AppendLine($"   ⚠️ Needs improvement in: {string.Join(", ", perf.WeakCategories)}");
                }
            }

            // Top Priority Recommendations
            sb.AppendLine("\n🎯 TOP PRIORITIES");
            sb.AppendLine("Focus on these items in the next session:");

            int priority = 1;
            foreach (var item in report.TopPriorityItems.Take(5))
            {
                sb.AppendLine($"{priority}. Practice {item.Category} '{item.ItemName}' - Focus on {item.WeakestActivity} activities");
                priority++;
            }

            // Activity Recommendations
            if (report.ActivityRecommendations.Any())
            {
                sb.AppendLine("\n💡 RECOMMENDED ACTIVITIES");
                foreach (var rec in report.ActivityRecommendations.Take(3))
                {
                    sb.AppendLine($"• {rec.ActivityType}: {rec.Reason}");
                    sb.AppendLine($"  Suggested items: {string.Join(", ", rec.SuggestedItems.Take(3))}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate weakness analysis summary for parents
        /// </summary>
        public string GenerateWeaknessSummary(WeaknessAnalysis analysis)
        {
            var sb = new StringBuilder();

            sb.AppendLine("🔍 AREAS THAT NEED MORE PRACTICE");
            sb.AppendLine(new string('-', 40));

            if (analysis.WeakNumbers.Any())
                sb.AppendLine($"🔢 Numbers: Focus on {string.Join(", ", analysis.WeakNumbers.Take(3).Select(w => w.ItemName))}");

            if (analysis.WeakCapitalAlphabets.Any())
                sb.AppendLine($"🔤 Capital Letters: Practice {string.Join(", ", analysis.WeakCapitalAlphabets.Take(3).Select(w => w.ItemName))}");

            if (analysis.WeakSmallAlphabets.Any())
                sb.AppendLine($"🔡 Small Letters: Work on {string.Join(", ", analysis.WeakSmallAlphabets.Take(3).Select(w => w.ItemName))}");

            if (analysis.WeakShapes.Any())
                sb.AppendLine($"🔷 Shapes: Review {string.Join(", ", analysis.WeakShapes.Take(3).Select(w => w.ItemName))}");

            if (analysis.WeakColors.Any())
                sb.AppendLine($"🎨 Colors: Practice {string.Join(", ", analysis.WeakColors.Take(3).Select(w => w.ItemName))}");

            if (analysis.WeakestActivities.Any())
            {
                sb.AppendLine("\n🎯 ACTIVITIES TO FOCUS ON:");
                foreach (var activity in analysis.WeakestActivities.Take(3))
                {
                    sb.AppendLine($"   • {activity.ActivityType}: {activity.AverageSuccessRate:F0}% success rate");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate learning plan summary
        /// </summary>
        public string GenerateLearningPlanSummary(FocusedLearningPlan plan)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📅 7-DAY LEARNING PLAN (Generated: {plan.GeneratedDate:MMM dd})");
            sb.AppendLine(new string('-', 50));

            foreach (var day in plan.DailyFocusItems)
            {
                sb.AppendLine($"\n📍 Day {day.Day} ({day.Date:MMM dd}):");
                sb.AppendLine($"⏱️ Duration: {day.EstimatedDuration} minutes");

                foreach (var item in day.FocusItems)
                {
                    sb.AppendLine($"   • {GetCategoryEmoji(item.Category)} {item.Category} '{item.ItemName}' - {item.ActivityType}");
                    sb.AppendLine($"     🎯 Goal: Reach {item.TargetScore}% ({item.Reason})");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate activity weakness details report
        /// </summary>
        public string GenerateActivityWeaknessReport(Dictionary<string, ActivityWeaknessDetails> weaknessDetails)
        {
            var sb = new StringBuilder();

            sb.AppendLine("📊 DETAILED ACTIVITY WEAKNESS ANALYSIS");
            sb.AppendLine(new string('=', 50));

            foreach (var weakness in weaknessDetails.OrderBy(w => w.Value.OverallSuccessRate))
            {
                sb.AppendLine($"\n🎮 {weakness.Key} Activity:");
                sb.AppendLine($"   📈 Overall Success Rate: {weakness.Value.OverallSuccessRate:F1}%");
                sb.AppendLine($"   🔢 Total Attempts: {weakness.Value.TotalAttempts}");

                if (weakness.Value.TopWeakItems.Any())
                {
                    sb.AppendLine($"   ⚠️ Weakest Items: {string.Join(", ", weakness.Value.TopWeakItems.Take(3))}");
                }

                if (weakness.Value.WeakItemsByCategory.Any())
                {
                    sb.AppendLine("   📂 Weak Items by Category:");
                    foreach (var category in weakness.Value.WeakItemsByCategory)
                    {
                        if (category.Value.Any())
                        {
                            var weakItems = category.Value.Take(3).Select(i => $"{i.ItemName}({i.SuccessRate:F0}%)");
                            sb.AppendLine($"      {GetCategoryEmoji(category.Key)} {category.Key}: {string.Join(", ", weakItems)}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate simple daily summary
        /// </summary>
        public string GenerateDailySummary(ChildProgress progress)
        {
            var sb = new StringBuilder();
            var totalAttempts = 0;
            var totalTime = 0.0;
            var completedItems = 0;

            sb.AppendLine($"📅 DAILY SUMMARY - {progress.PeriodStart:MMM dd, yyyy}");
            sb.AppendLine(new string('-', 30));

            var allDicts = new[]
            {
                ("Numbers", progress.NumberProgressDict),
                ("Capital Letters", progress.CapitalAlphabetProgressDict),
                ("Small Letters", progress.SmallAlphabetProgressDict),
                ("Shapes", progress.ShapeProgressDict),
                ("Colors", progress.ColorProgressDict)
            };

            foreach (var (categoryName, dict) in allDicts)
            {
                if (dict != null && dict.Any())
                {
                    var categoryAttempts = dict.Values.Sum(p => _analysisLogic.GetTotalAttempts(p));
                    var categoryTime = dict.Values.Sum(p => _analysisLogic.GetTotalTimeSpent(p));
                    var categoryCompleted = dict.Values.Count(p => p.Completed);

                    if (categoryAttempts > 0)
                    {
                        sb.AppendLine($"\n{GetCategoryEmojiByName(categoryName)} {categoryName}:");
                        sb.AppendLine($"   Activities: {categoryAttempts}");
                        sb.AppendLine($"   Time: {TimeSpan.FromSeconds(categoryTime):mm\\:ss}");
                        sb.AppendLine($"   Completed: {categoryCompleted}");

                        totalAttempts += categoryAttempts;
                        totalTime += categoryTime;
                        completedItems += categoryCompleted;
                    }
                }
            }

            sb.AppendLine($"\n📊 TOTAL:");
            sb.AppendLine($"   Activities: {totalAttempts}");
            sb.AppendLine($"   Time: {TimeSpan.FromSeconds(totalTime):hh\\:mm\\:ss}");
            sb.AppendLine($"   Completed: {completedItems}");

            return sb.ToString();
        }

        /// <summary>
        /// Generate statistics summary
        /// </summary>
        public string GenerateStatisticsSummary(Repository.ChildStatistics stats)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"📈 STATISTICS SUMMARY ({stats.AnalysisPeriod} days)");
            sb.AppendLine(new string('=', 40));
            sb.AppendLine($"📅 Generated: {stats.GeneratedDate:MMM dd, yyyy HH:mm}");
            sb.AppendLine($"👤 Child ID: {stats.UserId}");
            sb.AppendLine();
            sb.AppendLine($"🗓️ Days Active: {stats.TotalDaysActive} out of {stats.AnalysisPeriod}");
            sb.AppendLine($"🎯 Activities Attempted: {stats.TotalActivitiesAttempted}");
            sb.AppendLine($"✅ Activities Completed: {stats.TotalActivitiesCompleted}");
            sb.AppendLine($"📊 Success Rate: {stats.OverallSuccessRate:F1}%");
            sb.AppendLine($"⏱️ Total Time: {TimeSpan.FromSeconds(stats.TotalTimeSpent):hh\\:mm\\:ss}");
            sb.AppendLine($"⏱️ Average Session Time: {TimeSpan.FromSeconds(stats.AverageSessionTime):mm\\:ss}");

            return sb.ToString();
        }

        #region Private Helper Methods

        private void CalculateOverallStats(ChildProgress progress, ParentProgressReport report)
        {
            var allDicts = new[]
            {
                progress.NumberProgressDict,
                progress.CapitalAlphabetProgressDict,
                progress.SmallAlphabetProgressDict,
                progress.ShapeProgressDict,
                progress.ColorProgressDict
            };

            int totalAttempts = 0;
            int totalSuccess = 0;
            double totalTime = 0;

            foreach (var dict in allDicts.Where(d => d != null))
            {
                foreach (var itemProgress in dict.Values)
                {
                    totalAttempts += _analysisLogic.GetTotalAttempts(itemProgress);
                    totalTime += _analysisLogic.GetTotalTimeSpent(itemProgress);

                    if (itemProgress.TracingCount > 0)
                        totalSuccess += itemProgress.TracingCompleteCount;

                    if (itemProgress.QuizCount > 0)
                        totalSuccess += itemProgress.QuizCount - itemProgress.QuizFailCount;
                }
            }

            report.TotalActivitiesCompleted = totalAttempts;
            report.TotalTimeSpent = totalTime;
            report.OverallSuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
        }

        private void CalculateCategoryProgress(ChildProgress progress, ParentProgressReport report, ECDGameActivityName category,
            Dictionary<string, ECDGamesActivityProgress> progressDict)
        {
            if (progressDict == null || progressDict.Count == 0) return;

            var summary = new CategoryProgressSummary
            {
                Category = category,
                TotalItems = _analysisLogic.GetExpectedItemCount(category),
                ItemsAttempted = progressDict.Count,
                ItemsCompleted = progressDict.Count(p => p.Value.Completed),
                WeakItems = new List<string>(),
                StrongItems = new List<string>()
            };

            double totalScore = 0;
            double totalTime = 0;
            int scoredItems = 0;

            foreach (var kvp in progressDict)
            {
                var itemProgress = kvp.Value;
                var score = _analysisLogic.CalculateOverallScore(itemProgress);

                totalTime += _analysisLogic.GetTotalTimeSpent(itemProgress);

                if (_analysisLogic.GetTotalAttempts(itemProgress) > 0)
                {
                    totalScore += score;
                    scoredItems++;

                    if (score < 70)
                        summary.WeakItems.Add(kvp.Key);
                    else if (score >= 90)
                        summary.StrongItems.Add(kvp.Key);
                }
            }

            summary.CompletionRate = (double)summary.ItemsCompleted / summary.TotalItems * 100;
            summary.SuccessRate = scoredItems > 0 ? totalScore / scoredItems : 0;
            summary.TimeSpent = totalTime;

            report.CategoryProgress[category] = summary;
        }

        private void CalculateActivityPerformance(ChildProgress progress, ParentProgressReport report)
        {
            var activityTypes = new[]
            {
                "Tracing", "Object Recognition", "Listening", "Text to Figure",
                "Figure to Text", "Counting", "Bubble Pop"
            };

            foreach (var activityType in activityTypes)
            {
                var stats = GatherDetailedActivityStats(progress, activityType);

                if (stats.TotalAttempts > 0)
                {
                    var performance = new ActivityPerformanceSummary
                    {
                        ActivityType = activityType,
                        TotalAttempts = stats.TotalAttempts,
                        SuccessRate = stats.SuccessRate,
                        AverageTime = stats.AverageTime,
                        NeedsImprovement = stats.SuccessRate < 70,
                        WeakCategories = stats.WeakCategories
                    };

                    report.ActivityPerformance[activityType] = performance;
                }
            }
        }

        private (double SuccessRate, int TotalAttempts, double AverageTime, List<string> WeakCategories)
            GatherDetailedActivityStats(ChildProgress progress, string activityType)
        {
            var categoryData = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            int totalAttempts = 0;
            int totalSuccess = 0;
            double totalTime = 0;
            var weakCategories = new List<string>();

            foreach (var (category, dict) in categoryData)
            {
                if (dict == null) continue;

                int categoryAttempts = 0;
                int categorySuccess = 0;

                foreach (var itemProgress in dict.Values)
                {
                    var (attempts, success, time) = GetActivitySpecificStats(itemProgress, activityType);

                    categoryAttempts += attempts;
                    categorySuccess += success;
                    totalAttempts += attempts;
                    totalSuccess += success;
                    totalTime += time;
                }

                if (categoryAttempts > 0 && (double)categorySuccess / categoryAttempts < 0.6)
                {
                    weakCategories.Add(category.ToString());
                }
            }

            double successRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
            double avgTime = totalAttempts > 0 ? totalTime / totalAttempts : 0;

            return (successRate, totalAttempts, avgTime, weakCategories);
        }

        private (int attempts, int success, double time) GetActivitySpecificStats(
            ECDGamesActivityProgress progress, string activityType)
        {
            switch (activityType)
            {
                case "Tracing":
                    return (progress.TracingCount, progress.TracingCompleteCount, progress.TracingTotalTime);
                case "Object Recognition":
                    return GetActivityStatsData(progress.ObjectRecognitionQuiz);
                case "Listening":
                    return GetActivityStatsData(progress.HearingQuiz);
                case "Text to Figure":
                    return GetActivityStatsData(progress.TextToFigureQuiz);
                case "Figure to Text":
                    return GetActivityStatsData(progress.FigureToTextQuiz);
                case "Counting":
                    return GetActivityStatsData(progress.CountingQuiz);
                case "Bubble Pop":
                    return GetActivityStatsData(progress.BubblePop);
                default:
                    return (0, 0, 0);
            }
        }

        private (int attempts, int success, double time) GetActivityStatsData(ActivityStats stats)
        {
            if (stats == null) return (0, 0, 0);
            return (stats.Count, stats.Count - stats.FailCount, stats.TotalTime);
        }

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

        private string GetCategoryEmojiByName(string categoryName)
        {
            switch (categoryName)
            {
                case "Numbers":
                    return "🔢";
                case "Capital Letters":
                    return "🔤";
                case "Small Letters":
                    return "🔡";
                case "Shapes":
                    return "🔷";
                case "Colors":
                    return "🎨";
                default:
                    return "📝";
            }
        }

        #endregion
    }

    /// <summary>
    /// Report export utilities
    /// </summary>
    public static class ReportExporter
    {
        /// <summary>
        /// Export report to CSV format
        /// </summary>
        public static string ExportProgressToCSV(ParentProgressReport report)
        {
            var sb = new StringBuilder();

            // Headers
            sb.AppendLine("Category,Total Items,Items Completed,Completion Rate,Success Rate,Time Spent");

            // Category data
            foreach (var category in report.CategoryProgress)
            {
                var cat = category.Value;
                sb.AppendLine($"{category.Key},{cat.TotalItems},{cat.ItemsCompleted},{cat.CompletionRate:F1},{cat.SuccessRate:F1},{cat.TimeSpent:F1}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export activity performance to CSV
        /// </summary>
        public static string ExportActivityPerformanceToCSV(ParentProgressReport report)
        {
            var sb = new StringBuilder();

            // Headers
            sb.AppendLine("Activity Type,Total Attempts,Success Rate,Average Time,Needs Improvement");

            // Activity data
            foreach (var activity in report.ActivityPerformance)
            {
                var perf = activity.Value;
                sb.AppendLine($"{activity.Key},{perf.TotalAttempts},{perf.SuccessRate:F1},{perf.AverageTime:F1},{perf.NeedsImprovement}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Export learning plan to CSV
        /// </summary>
        public static string ExportLearningPlanToCSV(FocusedLearningPlan plan)
        {
            var sb = new StringBuilder();

            // Headers
            sb.AppendLine("Day,Date,Category,Item Name,Activity Type,Target Score,Estimated Minutes,Reason");

            // Plan data
            foreach (var day in plan.DailyFocusItems)
            {
                foreach (var item in day.FocusItems)
                {
                    sb.AppendLine($"{day.Day},{day.Date:yyyy-MM-dd},{item.Category},{item.ItemName},{item.ActivityType},{item.TargetScore},{item.EstimatedMinutes},{item.Reason}");
                }
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// Report formatting utilities
    /// </summary>
    public static class ReportFormatter
    {
        /// <summary>
        /// Format time span for display
        /// </summary>
        public static string FormatTimeSpan(double seconds)
        {
            var timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.TotalHours >= 1)
                return timeSpan.ToString(@"h\:mm\:ss");
            else
                return timeSpan.ToString(@"mm\:ss");
        }

        /// <summary>
        /// Format percentage for display
        /// </summary>
        public static string FormatPercentage(double percentage)
        {
            return $"{percentage:F1}%";
        }

        /// <summary>
        /// Format score with color coding (for console output)
        /// </summary>
        public static string FormatScoreWithColor(double score)
        {
            if (score >= 90) return $"🟢 {score:F0}%";
            if (score >= 80) return $"🟡 {score:F0}%";
            if (score >= 70) return $"🟠 {score:F0}%";
            return $"🔴 {score:F0}%";
        }

        /// <summary>
        /// Get performance level description
        /// </summary>
        public static string GetPerformanceLevel(double score)
        {
            if (score >= 90) return "Excellent";
            if (score >= 80) return "Good";
            if (score >= 70) return "Average";
            if (score >= 60) return "Below Average";
            return "Needs Improvement";
        }
    }
}