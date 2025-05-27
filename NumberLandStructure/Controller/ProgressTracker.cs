using System;
using System.Collections.Generic;
using NumberLandStructure.Data;

namespace NumberLandStructure.Tracking
{
    #region Progress Tracking Data Models

    /// <summary>
    /// Complete progress overview for a child
    /// </summary>
    public class ProgressOverview
    {
        public string UserId { get; set; }
        public DateTime GeneratedDate { get; set; }
        public Dictionary<ECDGameActivityName, CategoryTracking> CategoryProgress { get; set; }
    }

    /// <summary>
    /// Category-level tracking information
    /// </summary>
    public class CategoryTracking
    {
        public ECDGameActivityName Category { get; set; }
        public int TotalExpected { get; set; }
        public int AttemptedCount { get; set; }
        public int CompletedCount { get; set; }
        public double CompletionRate { get; set; }
        public List<string> CompletedItems { get; set; } = new List<string>();
        public List<string> InProgressItems { get; set; } = new List<string>();
        public List<string> NotStartedItems { get; set; } = new List<string>();
    }

    /// <summary>
    /// Detailed view of a specific category
    /// </summary>
    public class CategoryDetailView
    {
        public ECDGameActivityName Category { get; set; }
        public List<string> ExpectedItems { get; set; }
        public List<ItemDetailView> ItemDetails { get; set; }
    }

    /// <summary>
    /// Detailed view of a specific item
    /// </summary>
    public class ItemDetailView
    {
        public string ItemName { get; set; }
        public bool IsAttempted { get; set; }
        public bool IsCompleted { get; set; }
        public double OverallScore { get; set; }
        public int TotalAttempts { get; set; }
        public double TotalTimeSpent { get; set; }
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, ActivityStatus> ActivityStatus { get; set; } = new Dictionary<string, ActivityStatus>();
    }

    /// <summary>
    /// Status of a specific activity for an item
    /// </summary>
    public class ActivityStatus
    {
        public bool IsAttempted { get; set; }
        public bool IsCompleted { get; set; }
        public double SuccessRate { get; set; }
        public int Attempts { get; set; }
        public double TotalTime { get; set; }
        public string Stars { get; set; } // For tracing activities
    }

    /// <summary>
    /// Completion summary across all categories
    /// </summary>
    public class CompletionSummary
    {
        public string UserId { get; set; }
        public DateTime GeneratedDate { get; set; }
        public Dictionary<ECDGameActivityName, CategoryCompletion> CategoryCompletions { get; set; }
    }

    /// <summary>
    /// Completion details for a category
    /// </summary>
    public class CategoryCompletion
    {
        public ECDGameActivityName Category { get; set; }
        public int TotalItems { get; set; }
        public int AttemptedItems { get; set; }
        public int CompletedItems { get; set; }
        public double CompletionPercentage { get; set; }
        public double AttemptPercentage { get; set; }
        public List<string> CompletedItemsList { get; set; } = new List<string>();
        public List<string> AttemptedItemsList { get; set; } = new List<string>();
        public List<string> NotAttemptedItemsList { get; set; } = new List<string>();
    }

    /// <summary>
    /// Activity-specific progress view
    /// </summary>
    public class ActivityProgressView
    {
        public string ActivityType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public Dictionary<ECDGameActivityName, List<ItemActivityResult>> CategoryResults { get; set; }
    }

    /// <summary>
    /// Result for a specific item in an activity
    /// </summary>
    public class ItemActivityResult
    {
        public string ItemName { get; set; }
        public int Attempts { get; set; }
        public int Successes { get; set; }
        public double SuccessRate { get; set; }
        public double TotalTime { get; set; }
        public double AverageTime { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime LastAttempt { get; set; }
    }

    #endregion

    #region Progress Tracker Class

    /// <summary>
    /// Tracks and displays progress for individual items and activities
    /// </summary>
    public class ProgressTracker
    {
        private readonly AnalysisLogic _analysisLogic;

        public ProgressTracker()
        {
            _analysisLogic = new AnalysisLogic();
        }

        /// <summary>
        /// Get complete progress overview for a child
        /// </summary>
        public ProgressOverview GetProgressOverview(ChildProgress progress)
        {
            var overview = new ProgressOverview
            {
                UserId = progress.UserId,
                GeneratedDate = DateTime.UtcNow,
                CategoryProgress = new Dictionary<ECDGameActivityName, CategoryTracking>()
            };

            // Track each category
            overview.CategoryProgress[ECDGameActivityName.Numbers] = TrackCategory(
                ECDGameActivityName.Numbers, progress.NumberProgressDict);

            overview.CategoryProgress[ECDGameActivityName.CapitalAlphabet] = TrackCategory(
                ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict);

            overview.CategoryProgress[ECDGameActivityName.SmallAlphabet] = TrackCategory(
                ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict);

            overview.CategoryProgress[ECDGameActivityName.Shapes] = TrackCategory(
                ECDGameActivityName.Shapes, progress.ShapeProgressDict);

            overview.CategoryProgress[ECDGameActivityName.Colors] = TrackCategory(
                ECDGameActivityName.Colors, progress.ColorProgressDict);

            return overview;
        }

        /// <summary>
        /// Get detailed view of a specific category (e.g., Numbers 1-10)
        /// </summary>
        public CategoryDetailView GetCategoryDetails(ChildProgress progress, ECDGameActivityName category)
        {
            var progressDict = GetProgressDictionary(progress, category);
            var expectedItems = GetExpectedItemsForCategory(category);

            var detailView = new CategoryDetailView
            {
                Category = category,
                ExpectedItems = expectedItems,
                ItemDetails = new List<ItemDetailView>()
            };

            foreach (var expectedItem in expectedItems)
            {
                var itemDetail = new ItemDetailView
                {
                    ItemName = expectedItem,
                    IsAttempted = progressDict?.ContainsKey(expectedItem) == true,
                    ActivityStatus = new Dictionary<string, ActivityStatus>()
                };

                if (itemDetail.IsAttempted)
                {
                    var itemProgress = progressDict[expectedItem];
                    itemDetail.OverallScore = _analysisLogic.CalculateOverallScore(itemProgress);
                    itemDetail.IsCompleted = itemProgress.Completed;
                    itemDetail.TotalAttempts = _analysisLogic.GetTotalAttempts(itemProgress);
                    itemDetail.TotalTimeSpent = _analysisLogic.GetTotalTimeSpent(itemProgress);
                    itemDetail.LastUpdated = progress.LastUpdated;

                    // Track individual activities
                    itemDetail.ActivityStatus["Tracing"] = GetTracingStatus(itemProgress);
                    itemDetail.ActivityStatus["Object Recognition"] = GetQuizStatus(itemProgress.ObjectRecognitionQuiz);
                    itemDetail.ActivityStatus["Listening"] = GetQuizStatus(itemProgress.HearingQuiz);
                    itemDetail.ActivityStatus["Text to Figure"] = GetQuizStatus(itemProgress.TextToFigureQuiz);
                    itemDetail.ActivityStatus["Figure to Text"] = GetQuizStatus(itemProgress.FigureToTextQuiz);
                    itemDetail.ActivityStatus["Counting"] = GetQuizStatus(itemProgress.CountingQuiz);
                    itemDetail.ActivityStatus["Bubble Pop"] = GetQuizStatus(itemProgress.BubblePop);
                }
                else
                {
                    // Item not attempted - set default values
                    itemDetail.OverallScore = 0;
                    itemDetail.IsCompleted = false;
                    itemDetail.TotalAttempts = 0;
                    itemDetail.TotalTimeSpent = 0;

                    // All activities not attempted
                    foreach (var activityType in new[] { "Tracing", "Object Recognition", "Listening",
                                                       "Text to Figure", "Figure to Text", "Counting", "Bubble Pop" })
                    {
                        itemDetail.ActivityStatus[activityType] = new ActivityStatus
                        {
                            IsAttempted = false,
                            IsCompleted = false,
                            SuccessRate = 0,
                            Attempts = 0,
                            TotalTime = 0
                        };
                    }
                }

                detailView.ItemDetails.Add(itemDetail);
            }

            return detailView;
        }

        /// <summary>
        /// Get completion status for all categories
        /// </summary>
        public CompletionSummary GetCompletionSummary(ChildProgress progress)
        {
            var summary = new CompletionSummary
            {
                UserId = progress.UserId,
                GeneratedDate = DateTime.UtcNow,
                CategoryCompletions = new Dictionary<ECDGameActivityName, CategoryCompletion>()
            };

            var categories = new[]
            {
                ECDGameActivityName.Numbers,
                ECDGameActivityName.CapitalAlphabet,
                ECDGameActivityName.SmallAlphabet,
                ECDGameActivityName.Shapes,
                ECDGameActivityName.Colors
            };

            foreach (var category in categories)
            {
                var progressDict = GetProgressDictionary(progress, category);
                var expectedItems = GetExpectedItemsForCategory(category);

                var completion = new CategoryCompletion
                {
                    Category = category,
                    TotalItems = expectedItems.Count,
                    AttemptedItems = progressDict?.Count ?? 0,
                    CompletedItems = progressDict?.Count(p => p.Value.Completed) ?? 0,
                    CompletedItemsList = progressDict?.Where(p => p.Value.Completed).Select(p => p.Key).ToList() ?? new List<string>(),
                    AttemptedItemsList = progressDict?.Keys.ToList() ?? new List<string>(),
                    NotAttemptedItemsList = expectedItems.Where(item => progressDict?.ContainsKey(item) != true).ToList()
                };

                completion.CompletionPercentage = (double)completion.CompletedItems / completion.TotalItems * 100;
                completion.AttemptPercentage = (double)completion.AttemptedItems / completion.TotalItems * 100;

                summary.CategoryCompletions[category] = completion;
            }

            return summary;
        }

        /// <summary>
        /// Get specific activity progress across all items
        /// </summary>
        public ActivityProgressView GetActivityProgress(ChildProgress progress, string activityType)
        {
            var activityView = new ActivityProgressView
            {
                ActivityType = activityType,
                GeneratedDate = DateTime.UtcNow,
                CategoryResults = new Dictionary<ECDGameActivityName, List<ItemActivityResult>>()
            };

            var categories = new[]
            {
                ECDGameActivityName.Numbers,
                ECDGameActivityName.CapitalAlphabet,
                ECDGameActivityName.SmallAlphabet,
                ECDGameActivityName.Shapes,
                ECDGameActivityName.Colors
            };

            foreach (var category in categories)
            {
                var progressDict = GetProgressDictionary(progress, category);
                var results = new List<ItemActivityResult>();

                if (progressDict != null)
                {
                    foreach (var kvp in progressDict)
                    {
                        var itemProgress = kvp.Value;
                        var (attempts, success, time) = GetActivitySpecificStats(itemProgress, activityType);

                        if (attempts > 0)
                        {
                            results.Add(new ItemActivityResult
                            {
                                ItemName = kvp.Key,
                                Attempts = attempts,
                                Successes = success,
                                SuccessRate = attempts > 0 ? (double)success / attempts * 100 : 0,
                                TotalTime = time,
                                AverageTime = attempts > 0 ? time / attempts : 0,
                                IsCompleted = success > 0,
                                LastAttempt = progress.LastUpdated
                            });
                        }
                    }
                }

                activityView.CategoryResults[category] = results.OrderBy(r => r.ItemName).ToList();
            }

            return activityView;
        }

        /// <summary>
        /// Generate a visual progress grid
        /// </summary>
        public string GenerateProgressGrid(ChildProgress progress, ECDGameActivityName category)
        {
            var categoryDetails = GetCategoryDetails(progress, category);
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"📊 {category} Progress Grid");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine();

            // Header
            sb.AppendLine("Item    | Status | Tracing | ObjRec | Listen | T2F | F2T | Count | Bubble | Overall");
            sb.AppendLine(new string('-', 80));

            foreach (var item in categoryDetails.ItemDetails.OrderBy(i => i.ItemName))
            {
                sb.Append($"{item.ItemName,-7} | ");

                // Overall status
                if (item.IsCompleted)
                    sb.Append("✅ Done | ");
                else if (item.IsAttempted)
                    sb.Append("🔄 Prog | ");
                else
                    sb.Append("⭕ None | ");

                // Individual activities
                var activities = new[] { "Tracing", "Object Recognition", "Listening",
                                       "Text to Figure", "Figure to Text", "Counting", "Bubble Pop" };
                var widths = new[] { 7, 6, 6, 3, 3, 5, 6 };

                for (int i = 0; i < activities.Length; i++)
                {
                    var activity = item.ActivityStatus[activities[i]];
                    var status = GetActivityStatusSymbol(activity);
                    sb.Append($"{status,widths[i]} | ");
                }

                // Overall score
                if (item.IsAttempted)
                    sb.Append($"{item.OverallScore:F0}%");
                else
                    sb.Append("--");

                sb.AppendLine();
            }

            sb.AppendLine();
            sb.AppendLine("Legend: ✅=Completed, 🔄=In Progress, ⭕=Not Started, ❌=Failed");

            return sb.ToString();
        }

        /// <summary>
        /// Generate detailed item report
        /// </summary>
        public string GenerateItemReport(ChildProgress progress, ECDGameActivityName category, string itemName)
        {
            var progressDict = GetProgressDictionary(progress, category);

            if (progressDict == null || !progressDict.ContainsKey(itemName))
            {
                return $"❌ No progress data found for {category} '{itemName}'";
            }

            var itemProgress = progressDict[itemName];
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"📋 Detailed Report: {category} '{itemName}'");
            sb.AppendLine(new string('=', 50));
            sb.AppendLine($"📅 Last Updated: {progress.LastUpdated:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"✅ Completed: {(itemProgress.Completed ? "Yes" : "No")}");
            sb.AppendLine($"📊 Overall Score: {_analysisLogic.CalculateOverallScore(itemProgress):F1}%");
            sb.AppendLine($"🔢 Total Attempts: {_analysisLogic.GetTotalAttempts(itemProgress)}");
            sb.AppendLine($"⏱️ Total Time: {TimeSpan.FromSeconds(_analysisLogic.GetTotalTimeSpent(itemProgress)):mm\\:ss}");
            sb.AppendLine();

            // Tracing Details
            if (itemProgress.TracingCount > 0)
            {
                sb.AppendLine("📝 TRACING ACTIVITIES:");
                sb.AppendLine($"   • Attempts: {itemProgress.TracingCount}");
                sb.AppendLine($"   • Completed: {itemProgress.TracingCompleteCount}");
                sb.AppendLine($"   • Success Rate: {(double)itemProgress.TracingCompleteCount / itemProgress.TracingCount * 100:F1}%");
                sb.AppendLine($"   • Total Stars: {itemProgress.TotalStarsAchieved}/{itemProgress.TotalStars}");
                sb.AppendLine($"   • Total Time: {TimeSpan.FromSeconds(itemProgress.TracingTotalTime):mm\\:ss}");
                sb.AppendLine($"   • Average Time: {TimeSpan.FromSeconds(itemProgress.TracingTotalTime / itemProgress.TracingCount):mm\\:ss}");
                sb.AppendLine();
            }

            // Quiz Details
            if (itemProgress.QuizCount > 0)
            {
                sb.AppendLine("🎯 QUIZ ACTIVITIES:");
                sb.AppendLine($"   • Total Quizzes: {itemProgress.QuizCount}");
                sb.AppendLine($"   • Failed: {itemProgress.QuizFailCount}");
                sb.AppendLine($"   • Timeouts: {itemProgress.QuizTimeOutCount}");
                sb.AppendLine($"   • Success Rate: {(double)(itemProgress.QuizCount - itemProgress.QuizFailCount) / itemProgress.QuizCount * 100:F1}%");
                sb.AppendLine($"   • Total Time: {TimeSpan.FromSeconds(itemProgress.QuizTotalTime):mm\\:ss}");
                sb.AppendLine($"   • Average Time: {TimeSpan.FromSeconds(itemProgress.QuizTotalTime / itemProgress.QuizCount):mm\\:ss}");
                sb.AppendLine();

                // Individual quiz types
                var quizTypes = new[]
                {
                    ("Object Recognition", itemProgress.ObjectRecognitionQuiz),
                    ("Listening", itemProgress.HearingQuiz),
                    ("Text to Figure", itemProgress.TextToFigureQuiz),
                    ("Figure to Text", itemProgress.FigureToTextQuiz),
                    ("Counting", itemProgress.CountingQuiz),
                    ("Bubble Pop", itemProgress.BubblePop)
                };

                sb.AppendLine("📊 INDIVIDUAL QUIZ TYPES:");
                foreach (var (typeName, stats) in quizTypes)
                {
                    if (stats != null && stats.Count > 0)
                    {
                        sb.AppendLine($"   🎮 {typeName}:");
                        sb.AppendLine($"      • Attempts: {stats.Count}");
                        sb.AppendLine($"      • Failures: {stats.FailCount}");
                        sb.AppendLine($"      • Success Rate: {stats.SuccessRate:F1}%");
                        sb.AppendLine($"      • Total Time: {TimeSpan.FromSeconds(stats.TotalTime):mm\\:ss}");
                        sb.AppendLine($"      • Average Time: {TimeSpan.FromSeconds(stats.AverageTime):mm\\:ss}");
                    }
                }
            }

            return sb.ToString();
        }

        #region Private Helper Methods

        private CategoryTracking TrackCategory(ECDGameActivityName category, Dictionary<string, ECDGamesActivityProgress> progressDict)
        {
            var expectedItems = GetExpectedItemsForCategory(category);
            var tracking = new CategoryTracking
            {
                Category = category,
                TotalExpected = expectedItems.Count,
                AttemptedCount = progressDict?.Count ?? 0,
                CompletedCount = progressDict?.Count(p => p.Value.Completed) ?? 0,
                CompletedItems = progressDict?.Where(p => p.Value.Completed).Select(p => p.Key).ToList() ?? new List<string>(),
                InProgressItems = progressDict?.Where(p => !p.Value.Completed).Select(p => p.Key).ToList() ?? new List<string>(),
                NotStartedItems = expectedItems.Where(item => progressDict?.ContainsKey(item) != true).ToList()
            };

            tracking.CompletionRate = (double)tracking.CompletedCount / tracking.TotalExpected * 100;

            return tracking;
        }

        private ActivityStatus GetTracingStatus(ECDGamesActivityProgress itemProgress)
        {
            return new ActivityStatus
            {
                IsAttempted = itemProgress.TracingCount > 0,
                IsCompleted = itemProgress.TracingCompleteCount > 0,
                SuccessRate = itemProgress.TracingCount > 0 ? (double)itemProgress.TracingCompleteCount / itemProgress.TracingCount * 100 : 0,
                Attempts = itemProgress.TracingCount,
                TotalTime = itemProgress.TracingTotalTime,
                Stars = $"{itemProgress.TotalStarsAchieved}/{itemProgress.TotalStars}"
            };
        }

        private ActivityStatus GetQuizStatus(ActivityStats stats)
        {
            return new ActivityStatus
            {
                IsAttempted = stats?.Count > 0,
                IsCompleted = stats?.Count > stats?.FailCount,
                SuccessRate = stats?.SuccessRate ?? 0,
                Attempts = stats?.Count ?? 0,
                TotalTime = stats?.TotalTime ?? 0
            };
        }

        private string GetActivityStatusSymbol(ActivityStatus status)
        {
            if (!status.IsAttempted) return "⭕";
            if (status.SuccessRate >= 80) return "✅";
            if (status.SuccessRate >= 60) return "🔄";
            return "❌";
        }

        private Dictionary<string, ECDGamesActivityProgress> GetProgressDictionary(ChildProgress progress, ECDGameActivityName category)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers:
                    return progress.NumberProgressDict;
                case ECDGameActivityName.CapitalAlphabet:
                    return progress.CapitalAlphabetProgressDict;
                case ECDGameActivityName.SmallAlphabet:
                    return progress.SmallAlphabetProgressDict;
                case ECDGameActivityName.Shapes:
                    return progress.ShapeProgressDict;
                case ECDGameActivityName.Colors:
                    return progress.ColorProgressDict;
                default:
                    return null;
            }
        }

        private List<string> GetExpectedItemsForCategory(ECDGameActivityName category)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers:
                    return Enumerable.Range(1, 10).Select(i => i.ToString()).ToList();

                case ECDGameActivityName.CapitalAlphabet:
                    return Enumerable.Range('A', 26).Select(i => ((char)i).ToString()).ToList();

                case ECDGameActivityName.SmallAlphabet:
                    return Enumerable.Range('a', 26).Select(i => ((char)i).ToString()).ToList();

                case ECDGameActivityName.Shapes:
                    return new List<string> { "Circle", "Square", "Triangle", "Rectangle", "Oval",
                                            "Diamond", "Star", "Heart", "Pentagon", "Hexagon" };

                case ECDGameActivityName.Colors:
                    return new List<string> { "Red", "Blue", "Yellow", "Green", "Orange", "Purple",
                                            "Pink", "Brown", "Black", "White", "Gray", "Cyan" };

                default:
                    return new List<string>();
            }
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

        #endregion
    }

    #endregion
}