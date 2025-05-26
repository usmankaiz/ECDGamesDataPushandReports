//using MongoDB.Driver;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization.Attributes;
//using System.Threading.Tasks;

//namespace NumberLandStructure
//{
//    public enum ProgressEventType
//    {
//        Daily,
//        Weekly,
//        Monthly,
//        Yearly
//    }

//    public enum ECDGameActivityName
//    {
//        Numbers,
//        CapitalAlphabet,
//        SmallAlphabet,
//        Shapes,
//        Colors
//    }

//    public enum QuizTypeDetail
//    {
//        ObjectRecognition,
//        Counting,
//        FiguresToText,
//        TextToFigure,
//        Listening,
//        BubblePopActivity
//    }

//    /// <summary>
//    /// Main progress tracking class optimized for MongoDB
//    /// </summary>
//    [BsonIgnoreExtraElements]
//    public class ChildProgress
//    {
//        [BsonId]
//        [BsonRepresentation(BsonType.ObjectId)]
//        public string Id { get; set; }

//        /// <summary>
//        /// Child's user ID - indexed for queries
//        /// </summary>
//        [BsonElement("userId")]
//        public string UserId { get; set; }

//        /// <summary>
//        /// Time when this record was created
//        /// </summary>
//        [BsonElement("createdAt")]
//        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
//        public DateTime CreatedAt { get; set; }

//        /// <summary>
//        /// Timestamp for the start of this period
//        /// </summary>
//        [BsonElement("periodStart")]
//        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
//        public DateTime PeriodStart { get; set; }

//        /// <summary>
//        /// Daily, Weekly, Monthly, Yearly
//        /// </summary>
//        [BsonElement("eventType")]
//        [BsonRepresentation(BsonType.String)]
//        public ProgressEventType EventType { get; set; }

//        /// <summary>
//        /// Dictionary to store progress by number (1, 2, 3, etc.)
//        /// </summary>
//        [BsonElement("numberProgress")]
//        [BsonIgnoreIfNull]
//        public Dictionary<string, ECDGamesActivityProgress> NumberProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

//        /// <summary>
//        /// Dictionary to store progress by Capital Alphabets (A, B, C, etc.)
//        /// </summary>
//        [BsonElement("capitalAlphabetProgress")]
//        [BsonIgnoreIfNull]
//        public Dictionary<string, ECDGamesActivityProgress> CapitalAlphabetProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

//        /// <summary>
//        /// Dictionary to store progress by Small Alphabets (a, b, c, etc.)
//        /// </summary>
//        [BsonElement("smallAlphabetProgress")]
//        [BsonIgnoreIfNull]
//        public Dictionary<string, ECDGamesActivityProgress> SmallAlphabetProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

//        /// <summary>
//        /// Dictionary to store progress by shapes
//        /// </summary>
//        [BsonElement("shapeProgress")]
//        [BsonIgnoreIfNull]
//        public Dictionary<string, ECDGamesActivityProgress> ShapeProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

//        /// <summary>
//        /// Dictionary to store progress by colors
//        /// </summary>
//        [BsonElement("colorProgress")]
//        [BsonIgnoreIfNull]
//        public Dictionary<string, ECDGamesActivityProgress> ColorProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

//        /// <summary>
//        /// Last updated timestamp for tracking modifications
//        /// </summary>
//        [BsonElement("lastUpdated")]
//        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
//        public DateTime LastUpdated { get; set; }

//        /// <summary>
//        /// Add tracing activity data
//        /// </summary>
//        public void AddTracingActivity(TracingActivityInput input)
//        {
//            var progressDict = GetProgressDictionary(input.ActivityType);
//            if (progressDict == null) return;

//            if (!progressDict.ContainsKey(input.ItemDetails))
//            {
//                progressDict[input.ItemDetails] = new ECDGamesActivityProgress
//                {
//                    ItemName = input.ItemDetails
//                };
//            }

//            var progress = progressDict[input.ItemDetails];
//            progress.TracingCount++;

//            if (input.Completed)
//            {
//                progress.TracingCompleteCount++;
//                progress.Completed = true;
//            }

//            progress.TracingTotalTime += input.TotalTime;
//            progress.TotalStars += input.MaxStars;
//            progress.TotalStarsAchieved += input.StarsAchieved;

//            LastUpdated = DateTime.UtcNow;
//        }

//        /// <summary>
//        /// Add quiz activity data
//        /// </summary>
//        public void AddQuizActivity(QuizActivityInput input)
//        {
//            var progressDict = GetProgressDictionary(input.ActivityType);
//            if (progressDict == null) return;

//            if (!progressDict.ContainsKey(input.ItemDetails))
//            {
//                progressDict[input.ItemDetails] = new ECDGamesActivityProgress
//                {
//                    ItemName = input.ItemDetails
//                };
//            }

//            var progress = progressDict[input.ItemDetails];

//            foreach (var quiz in input.QuizDetails)
//            {
//                var activityStats = GetActivityStats(progress, quiz.Type);
//                if (activityStats == null) continue;

//                activityStats.Count++;
//                activityStats.TotalTime += quiz.TimeTaken;

//                // Check if quiz failed
//                bool failed = quiz.LivesRemaining == 0 || quiz.Score < 50 || quiz.QuizTimeOut;

//                if (failed)
//                {
//                    activityStats.FailCount++;
//                    activityStats.TotalFailTime += quiz.TimeTaken;
//                }

//                if (quiz.QuizTimeOut)
//                {
//                    activityStats.QuizTimeOutCount++;
//                }

//                // Update generic quiz stats
//                progress.QuizCount++;
//                progress.QuizTotalTime += quiz.TimeTaken;

//                if (failed)
//                {
//                    progress.QuizFailCount++;
//                }

//                if (quiz.QuizTimeOut)
//                {
//                    progress.QuizTimeOutCount++;
//                }
//            }

//            // Mark as completed if all quizzes passed with good scores
//            if (input.QuizDetails.All(q => q.Score >= 80))
//            {
//                progress.Completed = true;
//            }

//            LastUpdated = DateTime.UtcNow;
//        }

//        /// <summary>
//        /// Get the appropriate progress dictionary based on activity type
//        /// </summary>
//        public Dictionary<string, ECDGamesActivityProgress> GetProgressDictionary(ECDGameActivityName activityType)
//        {
//            switch (activityType)
//            {
//                case ECDGameActivityName.Numbers:
//                    return NumberProgressDict;
//                case ECDGameActivityName.CapitalAlphabet:
//                    return CapitalAlphabetProgressDict;
//                case ECDGameActivityName.SmallAlphabet:
//                    return SmallAlphabetProgressDict;
//                case ECDGameActivityName.Shapes:
//                    return ShapeProgressDict;
//                case ECDGameActivityName.Colors:
//                    return ColorProgressDict;
//                default:
//                    return null;
//            }
//        }

//        /// <summary>
//        /// Get the appropriate activity stats based on quiz type
//        /// </summary>
//        public ActivityStats GetActivityStats(ECDGamesActivityProgress progress, QuizTypeDetail quizType)
//        {
//            switch (quizType)
//            {
//                case QuizTypeDetail.Listening:
//                    return progress.HearingQuiz;
//                case QuizTypeDetail.TextToFigure:
//                    return progress.TextToFigureQuiz;
//                case QuizTypeDetail.FiguresToText:
//                    return progress.FigureToTextQuiz;
//                case QuizTypeDetail.Counting:
//                    return progress.CountingQuiz;
//                case QuizTypeDetail.BubblePopActivity:
//                    return progress.BubblePop;
//                case QuizTypeDetail.ObjectRecognition:
//                    return progress.ObjectRecognitionQuiz;
//                default:
//                    return null;
//            }
//        }

//        /// <summary>
//        /// Generate comprehensive parent progress report
//        /// </summary>
//        public ParentProgressReport GenerateParentReport()
//        {
//            var report = new ParentProgressReport
//            {
//                ChildUserId = UserId,
//                ReportDate = DateTime.UtcNow,
//                ReportPeriod = EventType,
//                CategoryProgress = new Dictionary<ECDGameActivityName, CategoryProgressSummary>(),
//                ActivityPerformance = new Dictionary<string, ActivityPerformanceSummary>()
//            };

//            // Calculate overall stats
//            CalculateOverallStats(report);

//            // Calculate category progress
//            CalculateCategoryProgress(report, ECDGameActivityName.Numbers, NumberProgressDict);
//            CalculateCategoryProgress(report, ECDGameActivityName.CapitalAlphabet, CapitalAlphabetProgressDict);
//            CalculateCategoryProgress(report, ECDGameActivityName.SmallAlphabet, SmallAlphabetProgressDict);
//            CalculateCategoryProgress(report, ECDGameActivityName.Shapes, ShapeProgressDict);
//            CalculateCategoryProgress(report, ECDGameActivityName.Colors, ColorProgressDict);

//            // Calculate activity performance
//            CalculateActivityPerformance(report);

//            // Get weakness analysis and recommendations
//            var weaknessAnalysis = GetWeaknessAnalysis();
//            report.TopPriorityItems = weaknessAnalysis.GetPrioritizedFocusItems();
//            report.ActivityRecommendations = GenerateActivityRecommendations(weaknessAnalysis);

//            return report;
//        }

//        private void CalculateOverallStats(ParentProgressReport report)
//        {
//            var allDicts = new[]
//            {
//                NumberProgressDict,
//                CapitalAlphabetProgressDict,
//                SmallAlphabetProgressDict,
//                ShapeProgressDict,
//                ColorProgressDict
//            };

//            int totalAttempts = 0;
//            int totalSuccess = 0;
//            double totalTime = 0;

//            foreach (var dict in allDicts.Where(d => d != null))
//            {
//                foreach (var progress in dict.Values)
//                {
//                    totalAttempts += progress.GetTotalAttempts();
//                    totalTime += progress.GetTotalTimeSpent();

//                    // Calculate success from various activities
//                    if (progress.TracingCount > 0)
//                    {
//                        totalSuccess += progress.TracingCompleteCount;
//                    }
//                    if (progress.QuizCount > 0)
//                    {
//                        totalSuccess += progress.QuizCount - progress.QuizFailCount;
//                    }
//                }
//            }

//            report.TotalActivitiesCompleted = totalAttempts;
//            report.TotalTimeSpent = totalTime;
//            report.OverallSuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
//        }

//        private void CalculateCategoryProgress(ParentProgressReport report, ECDGameActivityName category,
//            Dictionary<string, ECDGamesActivityProgress> progressDict)
//        {
//            if (progressDict == null || progressDict.Count == 0) return;

//            var summary = new CategoryProgressSummary
//            {
//                Category = category,
//                TotalItems = GetExpectedItemCount(category),
//                ItemsAttempted = progressDict.Count,
//                ItemsCompleted = progressDict.Count(p => p.Value.Completed),
//                WeakItems = new List<string>(),
//                StrongItems = new List<string>()
//            };

//            double totalScore = 0;
//            double totalTime = 0;
//            int scoredItems = 0;

//            foreach (var kvp in progressDict)
//            {
//                var progress = kvp.Value;
//                var score = progress.GetOverallScore();

//                totalTime += progress.GetTotalTimeSpent();

//                if (progress.GetTotalAttempts() > 0)
//                {
//                    totalScore += score;
//                    scoredItems++;

//                    if (score < 70)
//                        summary.WeakItems.Add(kvp.Key);
//                    else if (score >= 90)
//                        summary.StrongItems.Add(kvp.Key);
//                }
//            }

//            summary.CompletionRate = (double)summary.ItemsCompleted / summary.TotalItems * 100;
//            summary.SuccessRate = scoredItems > 0 ? totalScore / scoredItems : 0;
//            summary.TimeSpent = totalTime;

//            report.CategoryProgress[category] = summary;
//        }

//        private void CalculateActivityPerformance(ParentProgressReport report)
//        {
//            var activityTypes = new[]
//            {
//                "Tracing", "Object Recognition", "Listening", "Text to Figure",
//                "Figure to Text", "Counting", "Bubble Pop"
//            };

//            foreach (var activityType in activityTypes)
//            {
//                var stats = GatherDetailedActivityStats(activityType);

//                if (stats.TotalAttempts > 0)
//                {
//                    var performance = new ActivityPerformanceSummary
//                    {
//                        ActivityType = activityType,
//                        TotalAttempts = stats.TotalAttempts,
//                        SuccessRate = stats.SuccessRate,
//                        AverageTime = stats.AverageTime,
//                        NeedsImprovement = stats.SuccessRate < 70,
//                        WeakCategories = stats.WeakCategories
//                    };

//                    report.ActivityPerformance[activityType] = performance;
//                }
//            }
//        }

//        private (double SuccessRate, int TotalAttempts, double AverageTime, List<string> WeakCategories)
//            GatherDetailedActivityStats(string activityType)
//        {
//            var categoryData = new[]
//            {
//                (ECDGameActivityName.Numbers, NumberProgressDict),
//                (ECDGameActivityName.CapitalAlphabet, CapitalAlphabetProgressDict),
//                (ECDGameActivityName.SmallAlphabet, SmallAlphabetProgressDict),
//                (ECDGameActivityName.Shapes, ShapeProgressDict),
//                (ECDGameActivityName.Colors, ColorProgressDict)
//            };

//            int totalAttempts = 0;
//            int totalSuccess = 0;
//            double totalTime = 0;
//            var weakCategories = new List<string>();

//            foreach (var (category, dict) in categoryData)
//            {
//                if (dict == null) continue;

//                int categoryAttempts = 0;
//                int categorySuccess = 0;

//                foreach (var progress in dict.Values)
//                {
//                    var (attempts, success, time) = GetActivitySpecificStats(progress, activityType);

//                    categoryAttempts += attempts;
//                    categorySuccess += success;
//                    totalAttempts += attempts;
//                    totalSuccess += success;
//                    totalTime += time;
//                }

//                // Mark category as weak if success rate < 60%
//                if (categoryAttempts > 0 && (double)categorySuccess / categoryAttempts < 0.6)
//                {
//                    weakCategories.Add(category.ToString());
//                }
//            }

//            double successRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
//            double avgTime = totalAttempts > 0 ? totalTime / totalAttempts : 0;

//            return (successRate, totalAttempts, avgTime, weakCategories);
//        }

//        public (int attempts, int success, double time) GetActivitySpecificStats(
//            ECDGamesActivityProgress progress, string activityType)
//        {
//            switch (activityType)
//            {
//                case "Tracing":
//                    return (progress.TracingCount, progress.TracingCompleteCount, progress.TracingTotalTime);
//                case "Object Recognition":
//                    return GetActivityStatsData(progress.ObjectRecognitionQuiz);
//                case "Listening":
//                    return GetActivityStatsData(progress.HearingQuiz);
//                case "Text to Figure":
//                    return GetActivityStatsData(progress.TextToFigureQuiz);
//                case "Figure to Text":
//                    return GetActivityStatsData(progress.FigureToTextQuiz);
//                case "Counting":
//                    return GetActivityStatsData(progress.CountingQuiz);
//                case "Bubble Pop":
//                    return GetActivityStatsData(progress.BubblePop);
//                default:
//                    return (0, 0, 0);
//            }
//        }

//        private (int attempts, int success, double time) GetActivityStatsData(ActivityStats stats)
//        {
//            if (stats == null) return (0, 0, 0);
//            return (stats.Count, stats.Count - stats.FailCount, stats.TotalTime);
//        }

//        private List<ActivityRecommendation> GenerateActivityRecommendations(WeaknessAnalysis analysis)
//        {
//            var recommendations = new List<ActivityRecommendation>();

//            // Recommend based on weakest activities
//            foreach (var weakness in analysis.WeakestActivities.Take(3))
//            {
//                var items = GetItemsForWeakActivity(weakness.ActivityType);

//                recommendations.Add(new ActivityRecommendation
//                {
//                    ActivityType = weakness.ActivityType,
//                    Reason = $"Success rate is only {weakness.AverageSuccessRate:F0}%",
//                    SuggestedItems = items,
//                    Priority = weakness.AverageSuccessRate < 50 ? 1 : 2
//                });
//            }

//            // Add recommendations for categories with low completion
//            var categoryProgress = new[]
//            {
//                (ECDGameActivityName.Numbers, NumberProgressDict),
//                (ECDGameActivityName.CapitalAlphabet, CapitalAlphabetProgressDict),
//                (ECDGameActivityName.SmallAlphabet, SmallAlphabetProgressDict),
//                (ECDGameActivityName.Shapes, ShapeProgressDict),
//                (ECDGameActivityName.Colors, ColorProgressDict)
//            };

//            foreach (var (category, dict) in categoryProgress)
//            {
//                if (dict == null) continue;

//                var completionRate = (double)dict.Count(p => p.Value.Completed) / GetExpectedItemCount(category) * 100;

//                if (completionRate < 30)
//                {
//                    recommendations.Add(new ActivityRecommendation
//                    {
//                        ActivityType = "Completion Focus",
//                        Reason = $"Only {completionRate:F0}% of {category} completed",
//                        SuggestedItems = GetUncompletedItems(dict),
//                        Priority = 1
//                    });
//                }
//            }

//            return recommendations.OrderBy(r => r.Priority).ToList();
//        }

//        private List<string> GetItemsForWeakActivity(string activityType)
//        {
//            var items = new List<string>();
//            var allDicts = new[]
//            {
//                (ECDGameActivityName.Numbers, NumberProgressDict),
//                (ECDGameActivityName.CapitalAlphabet, CapitalAlphabetProgressDict),
//                (ECDGameActivityName.SmallAlphabet, SmallAlphabetProgressDict),
//                (ECDGameActivityName.Shapes, ShapeProgressDict),
//                (ECDGameActivityName.Colors, ColorProgressDict)
//            };

//            foreach (var (category, dict) in allDicts)
//            {
//                if (dict == null) continue;

//                foreach (var kvp in dict)
//                {
//                    var progress = kvp.Value;
//                    var weakestActivity = progress.GetWeakestActivity();

//                    if (weakestActivity.Contains(activityType))
//                    {
//                        items.Add($"{category}: {kvp.Key}");
//                    }
//                }
//            }

//            return items.Take(5).ToList();
//        }

//        private List<string> GetUncompletedItems(Dictionary<string, ECDGamesActivityProgress> dict)
//        {
//            return dict
//                .Where(kvp => !kvp.Value.Completed)
//                .Select(kvp => kvp.Key)
//                .Take(5)
//                .ToList();
//        }

//        private int GetExpectedItemCount(ECDGameActivityName category)
//        {
//            switch (category)
//            {
//                case ECDGameActivityName.Numbers:
//                    return 10; // 1-10
//                case ECDGameActivityName.CapitalAlphabet:
//                    return 26; // A-Z
//                case ECDGameActivityName.SmallAlphabet:
//                    return 26; // a-z
//                case ECDGameActivityName.Shapes:
//                    return 10; // Assuming 10 basic shapes
//                case ECDGameActivityName.Colors:
//                    return 12; // Assuming 12 basic colors
//                default:
//                    return 10;
//            }
//        }

//        private List<WeakItem> GetWeakItems(Dictionary<string, ECDGamesActivityProgress> progressDict, string category, int topCount)
//        {
//            if (progressDict == null || progressDict.Count == 0)
//                return new List<WeakItem>();

//            return progressDict
//                .Select(kvp => new WeakItem
//                {
//                    Category = category,
//                    ItemName = kvp.Value.ItemName,
//                    OverallScore = kvp.Value.GetOverallScore(),
//                    WeakestActivity = kvp.Value.GetWeakestActivity(),
//                    Completed = kvp.Value.Completed,
//                    TotalAttempts = kvp.Value.GetTotalAttempts(),
//                    TotalTimeSpent = kvp.Value.GetTotalTimeSpent(),
//                    CompletionRate = kvp.Value.GetCompletionRate()
//                })
//                .Where(item => !item.Completed || item.OverallScore < 70)
//                .OrderBy(item => item.OverallScore)
//                .ThenBy(item => item.CompletionRate)
//                .Take(topCount)
//                .ToList();
//        }

//        private List<ActivityWeakness> GetWeakestActivities()
//        {
//            var activityTypes = new[]
//            {
//                "Tracing", "ObjectRecognition", "Hearing", "TextToFigure",
//                "FigureToText", "Counting", "BubblePop"
//            };

//            var weaknesses = new List<ActivityWeakness>();

//            foreach (var activityType in activityTypes)
//            {
//                var stats = GatherActivityStats(activityType);
//                if (stats.TotalAttempts > 0)
//                {
//                    weaknesses.Add(new ActivityWeakness
//                    {
//                        ActivityType = activityType,
//                        AverageSuccessRate = stats.SuccessRate,
//                        TotalAttempts = stats.TotalAttempts,
//                        TotalFailures = stats.TotalFailures,
//                        AverageTime = stats.AverageTime
//                    });
//                }
//            }

//            return weaknesses
//                .OrderBy(w => w.AverageSuccessRate)
//                .ThenByDescending(w => w.TotalFailures)
//                .Take(5)
//                .ToList();
//        }

//        private (double SuccessRate, int TotalAttempts, int TotalFailures, double AverageTime)
//            GatherActivityStats(string activityType)
//        {
//            var allDicts = new[]
//            {
//                NumberProgressDict,
//                CapitalAlphabetProgressDict,
//                SmallAlphabetProgressDict,
//                ShapeProgressDict,
//                ColorProgressDict
//            };

//            int totalAttempts = 0;
//            int totalSuccess = 0;
//            int totalFailures = 0;
//            double totalTime = 0;

//            foreach (var dict in allDicts)
//            {
//                if (dict == null) continue;

//                foreach (var progress in dict.Values)
//                {
//                    switch (activityType)
//                    {
//                        case "Tracing":
//                            totalAttempts += progress.TracingCount;
//                            totalSuccess += progress.TracingCompleteCount;
//                            totalFailures += progress.TracingCount - progress.TracingCompleteCount;
//                            totalTime += progress.TracingTotalTime;
//                            break;

//                        case "ObjectRecognition":
//                            AddActivityStatsData(progress.ObjectRecognitionQuiz);
//                            break;

//                        case "Hearing":
//                            AddActivityStatsData(progress.HearingQuiz);
//                            break;

//                        case "TextToFigure":
//                            AddActivityStatsData(progress.TextToFigureQuiz);
//                            break;

//                        case "FigureToText":
//                            AddActivityStatsData(progress.FigureToTextQuiz);
//                            break;

//                        case "Counting":
//                            AddActivityStatsData(progress.CountingQuiz);
//                            break;

//                        case "BubblePop":
//                            AddActivityStatsData(progress.BubblePop);
//                            break;
//                    }
//                }
//            }

//            void AddActivityStatsData(ActivityStats stats)
//            {
//                if (stats != null)
//                {
//                    totalAttempts += stats.Count;
//                    totalSuccess += stats.Count - stats.FailCount;
//                    totalFailures += stats.FailCount;
//                    totalTime += stats.TotalTime;
//                }
//            }

//            double successRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
//            double avgTime = totalAttempts > 0 ? totalTime / totalAttempts : 0;

//            return (successRate, totalAttempts, totalFailures, avgTime);
//        }

//        /// <summary>
//        /// Get weakness analysis results
//        /// </summary>
//        public WeaknessAnalysis GetWeaknessAnalysis(int topWeakItems = 5)
//        {
//            var analysis = new WeaknessAnalysis();

//            // Analyze each category
//            analysis.WeakNumbers = GetWeakItems(NumberProgressDict, "Numbers", topWeakItems);
//            analysis.WeakCapitalAlphabets = GetWeakItems(CapitalAlphabetProgressDict, "Capital Letters", topWeakItems);
//            analysis.WeakSmallAlphabets = GetWeakItems(SmallAlphabetProgressDict, "Small Letters", topWeakItems);
//            analysis.WeakShapes = GetWeakItems(ShapeProgressDict, "Shapes", topWeakItems);
//            analysis.WeakColors = GetWeakItems(ColorProgressDict, "Colors", topWeakItems);

//            // Get weakest activities across all items
//            analysis.WeakestActivities = GetWeakestActivities();

//            return analysis;
//        }
//    }

//    /// <summary>
//    /// Activity progress for each item (number, letter, shape, etc.)
//    /// </summary>
//    [BsonIgnoreExtraElements]
//    public class ECDGamesActivityProgress
//    {
//        [BsonElement("itemName")]
//        public string ItemName { get; set; }

//        [BsonElement("completed")]
//        public bool Completed { get; set; }

//        // Tracing activities
//        [BsonElement("tracingCount")]
//        public int TracingCount { get; set; }

//        [BsonElement("tracingCompleteCount")]
//        public int TracingCompleteCount { get; set; }

//        [BsonElement("tracingTotalTime")]
//        public double TracingTotalTime { get; set; }

//        [BsonElement("totalStars")]
//        public int TotalStars { get; set; }

//        [BsonElement("totalStarsAchieved")]
//        public int TotalStarsAchieved { get; set; }

//        // Regular quiz activities
//        [BsonElement("quizCount")]
//        public int QuizCount { get; set; }

//        [BsonElement("quizTotalTime")]
//        public double QuizTotalTime { get; set; }

//        [BsonElement("quizFailCount")]
//        public int QuizFailCount { get; set; }

//        [BsonElement("quizTimeOutCount")]
//        public int QuizTimeOutCount { get; set; }

//        // Individual activity types
//        [BsonElement("objectRecognitionQuiz")]
//        public ActivityStats ObjectRecognitionQuiz { get; set; } = new ActivityStats();

//        [BsonElement("hearingQuiz")]
//        public ActivityStats HearingQuiz { get; set; } = new ActivityStats();

//        [BsonElement("textToFigureQuiz")]
//        public ActivityStats TextToFigureQuiz { get; set; } = new ActivityStats();

//        [BsonElement("figureToTextQuiz")]
//        public ActivityStats FigureToTextQuiz { get; set; } = new ActivityStats();

//        [BsonElement("countingQuiz")]
//        public ActivityStats CountingQuiz { get; set; } = new ActivityStats();

//        [BsonElement("bubblePop")]
//        public ActivityStats BubblePop { get; set; } = new ActivityStats();

//        /// <summary>
//        /// Calculate overall score for this item
//        /// </summary>
//        public double GetOverallScore()
//        {
//            double totalScore = 0;
//            int components = 0;

//            // Tracing score
//            if (TotalStars > 0)
//            {
//                totalScore += (double)TotalStarsAchieved / TotalStars * 100;
//                components++;
//            }

//            // Quiz score
//            if (QuizCount > 0)
//            {
//                totalScore += (double)(QuizCount - QuizFailCount) / QuizCount * 100;
//                components++;
//            }

//            // Individual activity scores
//            var activities = new[]
//            {
//                ObjectRecognitionQuiz, HearingQuiz, TextToFigureQuiz,
//                FigureToTextQuiz, CountingQuiz, BubblePop
//            };

//            foreach (var activity in activities)
//            {
//                if (activity != null && activity.Count > 0)
//                {
//                    totalScore += activity.SuccessRate;
//                    components++;
//                }
//            }

//            return components > 0 ? totalScore / components : 0;
//        }

//        /// <summary>
//        /// Get the weakest activity for this item
//        /// </summary>
//        public string GetWeakestActivity()
//        {
//            var activities = new Dictionary<string, double>
//            {
//                ["Tracing"] = TracingCount > 0 ? (double)TracingCompleteCount / TracingCount * 100 : 100,
//                ["Object Recognition"] = ObjectRecognitionQuiz?.SuccessRate ?? 100,
//                ["Hearing"] = HearingQuiz?.SuccessRate ?? 100,
//                ["Text to Figure"] = TextToFigureQuiz?.SuccessRate ?? 100,
//                ["Figure to Text"] = FigureToTextQuiz?.SuccessRate ?? 100,
//                ["Counting"] = CountingQuiz?.SuccessRate ?? 100,
//                ["Bubble Pop"] = BubblePop?.SuccessRate ?? 100
//            };

//            return activities.OrderBy(a => a.Value).First().Key;
//        }

//        public int GetTotalAttempts()
//        {
//            return TracingCount + QuizCount;
//        }

//        public double GetTotalTimeSpent()
//        {
//            return TracingTotalTime + QuizTotalTime;
//        }

//        public double GetCompletionRate()
//        {
//            if (TracingCount == 0 && QuizCount == 0) return 0;

//            double tracingRate = TracingCount > 0 ? (double)TracingCompleteCount / TracingCount : 0;
//            double quizRate = QuizCount > 0 ? (double)(QuizCount - QuizFailCount) / QuizCount : 0;

//            return ((tracingRate + quizRate) / 2) * 100;
//        }
//    }

//    /// <summary>
//    /// Statistics for individual activity types
//    /// </summary>
//    [BsonIgnoreExtraElements]
//    public class ActivityStats
//    {
//        [BsonElement("count")]
//        public int Count { get; set; }

//        [BsonElement("failCount")]
//        public int FailCount { get; set; }

//        [BsonElement("totalTime")]
//        public double TotalTime { get; set; }

//        [BsonElement("totalFailTime")]
//        public double TotalFailTime { get; set; }

//        [BsonElement("quizTimeOutCount")]
//        public int QuizTimeOutCount { get; set; }

//        // Success rate as a percentage
//        [BsonIgnore]
//        public double SuccessRate => Count > 0 ? ((double)(Count - FailCount) / Count) * 100 : 0;

//        // Average time per attempt in seconds
//        [BsonIgnore]
//        public double AverageTime => Count > 0 ? TotalTime / Count : 0;
//    }

//    /// <summary>
//    /// Input model for tracing activities
//    /// </summary>
//    public class TracingActivityInput
//    {
//        public ECDGameActivityName ActivityType { get; set; }
//        public string ItemDetails { get; set; }
//        public bool Completed { get; set; }
//        public int StarsAchieved { get; set; }
//        public int MaxStars { get; set; } = 3; // Default max stars
//        public double TotalTime { get; set; }
//    }

//    /// <summary>
//    /// Input model for quiz activities
//    /// </summary>
//    public class QuizActivityInput
//    {
//        public ECDGameActivityName ActivityType { get; set; }
//        public string ItemDetails { get; set; }
//        public List<QuizDetail> QuizDetails { get; set; }
//    }

//    /// <summary>
//    /// Quiz detail information
//    /// </summary>
//    public class QuizDetail
//    {
//        public QuizTypeDetail Type { get; set; }
//        public bool Completed { get; set; }
//        public int TotalLives { get; set; }
//        public int LivesRemaining { get; set; }
//        public bool QuizTimeOut { get; set; }
//        public double TimeTaken { get; set; }
//        public double TimeGiven { get; set; }
//        public int Score { get; set; } // 0-100
//    }

//    /// <summary>
//    /// Weakness analysis results
//    /// </summary>
//    public class WeaknessAnalysis
//    {
//        public List<WeakItem> WeakNumbers { get; set; } = new List<WeakItem>();
//        public List<WeakItem> WeakCapitalAlphabets { get; set; } = new List<WeakItem>();
//        public List<WeakItem> WeakSmallAlphabets { get; set; } = new List<WeakItem>();
//        public List<WeakItem> WeakShapes { get; set; } = new List<WeakItem>();
//        public List<WeakItem> WeakColors { get; set; } = new List<WeakItem>();
//        public List<ActivityWeakness> WeakestActivities { get; set; } = new List<ActivityWeakness>();

//        /// <summary>
//        /// Get a parent-friendly summary
//        /// </summary>
//        public string GetParentSummary()
//        {
//            var summary = new System.Text.StringBuilder();

//            summary.AppendLine("Areas that need more practice:");

//            if (WeakNumbers.Any())
//                summary.AppendLine($"• Numbers: Focus on {string.Join(", ", WeakNumbers.Take(3).Select(w => w.ItemName))}");

//            if (WeakCapitalAlphabets.Any())
//                summary.AppendLine($"• Capital Letters: Practice {string.Join(", ", WeakCapitalAlphabets.Take(3).Select(w => w.ItemName))}");

//            if (WeakSmallAlphabets.Any())
//                summary.AppendLine($"• Small Letters: Work on {string.Join(", ", WeakSmallAlphabets.Take(3).Select(w => w.ItemName))}");

//            if (WeakShapes.Any())
//                summary.AppendLine($"• Shapes: Review {string.Join(", ", WeakShapes.Take(3).Select(w => w.ItemName))}");

//            if (WeakColors.Any())
//                summary.AppendLine($"• Colors: Practice {string.Join(", ", WeakColors.Take(3).Select(w => w.ItemName))}");

//            if (WeakestActivities.Any())
//            {
//                summary.AppendLine("\nActivities to focus on:");
//                foreach (var activity in WeakestActivities.Take(3))
//                {
//                    summary.AppendLine($"• {activity.ActivityType}: {activity.AverageSuccessRate:F0}% success rate");
//                }
//            }

//            return summary.ToString();
//        }

//        /// <summary>
//        /// Get prioritized items to focus on across all categories
//        /// </summary>
//        public List<FocusItem> GetPrioritizedFocusItems(int maxItems = 10)
//        {
//            var allWeakItems = new List<FocusItem>();

//            // Add all weak items with their category context
//            allWeakItems.AddRange(WeakNumbers.Select(w => new FocusItem
//            {
//                Category = ECDGameActivityName.Numbers,
//                ItemName = w.ItemName,
//                Score = w.OverallScore,
//                WeakestActivity = w.WeakestActivity,
//                Priority = CalculatePriority(w)
//            }));

//            allWeakItems.AddRange(WeakCapitalAlphabets.Select(w => new FocusItem
//            {
//                Category = ECDGameActivityName.CapitalAlphabet,
//                ItemName = w.ItemName,
//                Score = w.OverallScore,
//                WeakestActivity = w.WeakestActivity,
//                Priority = CalculatePriority(w)
//            }));

//            allWeakItems.AddRange(WeakSmallAlphabets.Select(w => new FocusItem
//            {
//                Category = ECDGameActivityName.SmallAlphabet,
//                ItemName = w.ItemName,
//                Score = w.OverallScore,
//                WeakestActivity = w.WeakestActivity,
//                Priority = CalculatePriority(w)
//            }));

//            allWeakItems.AddRange(WeakShapes.Select(w => new FocusItem
//            {
//                Category = ECDGameActivityName.Shapes,
//                ItemName = w.ItemName,
//                Score = w.OverallScore,
//                WeakestActivity = w.WeakestActivity,
//                Priority = CalculatePriority(w)
//            }));

//            allWeakItems.AddRange(WeakColors.Select(w => new FocusItem
//            {
//                Category = ECDGameActivityName.Colors,
//                ItemName = w.ItemName,
//                Score = w.OverallScore,
//                WeakestActivity = w.WeakestActivity,
//                Priority = CalculatePriority(w)
//            }));

//            // Return top priority items
//            return allWeakItems
//                .OrderByDescending(item => item.Priority)
//                .Take(maxItems)
//                .ToList();
//        }

//        private double CalculatePriority(WeakItem item)
//        {
//            // Priority formula: lower score = higher priority
//            // Bonus priority for items with very few attempts
//            double priorityScore = 100 - item.OverallScore;

//            if (item.TotalAttempts < 3)
//                priorityScore += 20; // Boost priority for rarely attempted items

//            if (!item.Completed)
//                priorityScore += 10; // Boost priority for incomplete items

//            return priorityScore;
//        }
//    }

//    public class WeakItem
//    {
//        public string Category { get; set; }
//        public string ItemName { get; set; }
//        public double OverallScore { get; set; }
//        public string WeakestActivity { get; set; }
//        public bool Completed { get; set; }
//        public int TotalAttempts { get; set; }
//        public double TotalTimeSpent { get; set; }
//        public double CompletionRate { get; set; }
//    }

//    public class ActivityWeakness
//    {
//        public string ActivityType { get; set; }
//        public double AverageSuccessRate { get; set; }
//        public int TotalAttempts { get; set; }
//        public int TotalFailures { get; set; }
//        public double AverageTime { get; set; }
//        public Dictionary<ECDGameActivityName, List<string>> WeakItemsByCategory { get; set; } = new Dictionary<ECDGameActivityName, List<string>>();
//    }

//    /// <summary>
//    /// Focus item with priority score
//    /// </summary>
//    public class FocusItem
//    {
//        public ECDGameActivityName Category { get; set; }
//        public string ItemName { get; set; }
//        public double Score { get; set; }
//        public string WeakestActivity { get; set; }
//        public double Priority { get; set; }

//        public string GetRecommendation()
//        {
//            return $"Practice {Category} '{ItemName}' - Focus on {WeakestActivity} activities";
//        }
//    }

//    /// <summary>
//    /// Detailed progress report for parents
//    /// </summary>
//    public class ParentProgressReport
//    {
//        public string ChildUserId { get; set; }
//        public DateTime ReportDate { get; set; }
//        public ProgressEventType ReportPeriod { get; set; }

//        // Overall stats
//        public int TotalActivitiesCompleted { get; set; }
//        public double TotalTimeSpent { get; set; }
//        public double OverallSuccessRate { get; set; }

//        // Category-wise progress
//        public Dictionary<ECDGameActivityName, CategoryProgressSummary> CategoryProgress { get; set; }

//        // Activity-wise performance
//        public Dictionary<string, ActivityPerformanceSummary> ActivityPerformance { get; set; }

//        // Recommendations
//        public List<FocusItem> TopPriorityItems { get; set; }
//        public List<ActivityRecommendation> ActivityRecommendations { get; set; }

//        /// <summary>
//        /// Generate a formatted report for parents
//        /// </summary>
//        public string GenerateReport()
//        {
//            var report = new System.Text.StringBuilder();

//            report.AppendLine($"Progress Report - {ReportDate:MMM dd, yyyy}");
//            report.AppendLine($"Period: {ReportPeriod}");
//            report.AppendLine(new string('-', 50));

//            // Overall Summary
//            report.AppendLine("\n📊 OVERALL SUMMARY");
//            report.AppendLine($"Total Activities: {TotalActivitiesCompleted}");
//            report.AppendLine($"Total Time: {TimeSpan.FromSeconds(TotalTimeSpent):hh\\:mm\\:ss}");
//            report.AppendLine($"Success Rate: {OverallSuccessRate:F1}%");

//            // Category Progress
//            report.AppendLine("\n📚 CATEGORY PROGRESS");
//            foreach (var category in CategoryProgress.OrderBy(c => c.Value.CompletionRate))
//            {
//                var cat = category.Value;
//                report.AppendLine($"\n{GetCategoryEmoji(category.Key)} {category.Key}:");
//                report.AppendLine($"   • Completion: {cat.CompletionRate:F0}% ({cat.ItemsCompleted}/{cat.TotalItems})");
//                report.AppendLine($"   • Success Rate: {cat.SuccessRate:F0}%");
//                report.AppendLine($"   • Time Spent: {TimeSpan.FromSeconds(cat.TimeSpent):mm\\:ss}");

//                if (cat.WeakItems.Any())
//                {
//                    report.AppendLine($"   • Need Practice: {string.Join(", ", cat.WeakItems.Take(3))}");
//                }
//            }

//            // Activity Performance
//            report.AppendLine("\n🎮 ACTIVITY PERFORMANCE");
//            foreach (var activity in ActivityPerformance.OrderBy(a => a.Value.SuccessRate))
//            {
//                var perf = activity.Value;
//                report.AppendLine($"\n{activity.Key}:");
//                report.AppendLine($"   • Success Rate: {perf.SuccessRate:F0}%");
//                report.AppendLine($"   • Attempts: {perf.TotalAttempts}");
//                report.AppendLine($"   • Avg Time: {perf.AverageTime:F1}s");

//                if (perf.NeedsImprovement)
//                {
//                    report.AppendLine($"   ⚠️ Needs improvement in: {string.Join(", ", perf.WeakCategories)}");
//                }
//            }

//            // Top Priority Recommendations
//            report.AppendLine("\n🎯 TOP PRIORITIES");
//            report.AppendLine("Focus on these items in the next session:");

//            int priority = 1;
//            foreach (var item in TopPriorityItems.Take(5))
//            {
//                report.AppendLine($"{priority}. {item.GetRecommendation()}");
//                priority++;
//            }

//            // Activity Recommendations
//            report.AppendLine("\n💡 RECOMMENDED ACTIVITIES");
//            foreach (var rec in ActivityRecommendations.Take(3))
//            {
//                report.AppendLine($"• {rec.ActivityType}: {rec.Reason}");
//                report.AppendLine($"  Suggested items: {string.Join(", ", rec.SuggestedItems.Take(3))}");
//            }

//            return report.ToString();
//        }

//        private string GetCategoryEmoji(ECDGameActivityName category)
//        {
//            switch (category)
//            {
//                case ECDGameActivityName.Numbers:
//                    return "🔢";
//                case ECDGameActivityName.CapitalAlphabet:
//                    return "🔤";
//                case ECDGameActivityName.SmallAlphabet:
//                    return "🔡";
//                case ECDGameActivityName.Shapes:
//                    return "🔷";
//                case ECDGameActivityName.Colors:
//                    return "🎨";
//                default:
//                    return "📝";
//            }
//        }
//    }

//    /// <summary>
//    /// Category-wise progress summary
//    /// </summary>
//    public class CategoryProgressSummary
//    {
//        public ECDGameActivityName Category { get; set; }
//        public int TotalItems { get; set; }
//        public int ItemsCompleted { get; set; }
//        public int ItemsAttempted { get; set; }
//        public double CompletionRate { get; set; }
//        public double SuccessRate { get; set; }
//        public double TimeSpent { get; set; }
//        public List<string> WeakItems { get; set; } = new List<string>();
//        public List<string> StrongItems { get; set; } = new List<string>();
//    }

//    /// <summary>
//    /// Activity performance summary
//    /// </summary>
//    public class ActivityPerformanceSummary
//    {
//        public string ActivityType { get; set; }
//        public int TotalAttempts { get; set; }
//        public double SuccessRate { get; set; }
//        public double AverageTime { get; set; }
//        public bool NeedsImprovement { get; set; }
//        public List<string> WeakCategories { get; set; } = new List<string>();
//    }

//    /// <summary>
//    /// Activity recommendations
//    /// </summary>
//    public class ActivityRecommendation
//    {
//        public string ActivityType { get; set; }
//        public string Reason { get; set; }
//        public List<string> SuggestedItems { get; set; } = new List<string>();
//        public int Priority { get; set; }
//    }

//    /// <summary>
//    /// MongoDB repository helper methods
//    /// </summary>
//    public class ChildProgressRepository
//    {
//        private readonly IMongoCollection<ChildProgress> _collection;

//        public ChildProgressRepository(IMongoDatabase database)
//        {
//            _collection = database.GetCollection<ChildProgress>("child_progress");
//            CreateIndexes();
//        }

//        private void CreateIndexes()
//        {
//            // Compound index for efficient queries
//            _collection.Indexes.CreateOne(new CreateIndexModel<ChildProgress>(
//                Builders<ChildProgress>.IndexKeys
//                    .Ascending(x => x.UserId)
//                    .Descending(x => x.PeriodStart)
//                    .Ascending(x => x.EventType)
//            ));

//            // Index for last updated queries
//            _collection.Indexes.CreateOne(new CreateIndexModel<ChildProgress>(
//                Builders<ChildProgress>.IndexKeys.Descending(x => x.LastUpdated)
//            ));
//        }

//        /// <summary>
//        /// Get or create progress record for today
//        /// </summary>
//        public async Task<ChildProgress> GetOrCreateDailyProgress(string userId)
//        {
//            var today = DateTime.UtcNow.Date;

//            var progress = await _collection.Find(x =>
//                x.UserId == userId &&
//                x.EventType == ProgressEventType.Daily &&
//                x.PeriodStart == today
//            ).FirstOrDefaultAsync();

//            if (progress == null)
//            {
//                progress = new ChildProgress
//                {
//                    UserId = userId,
//                    CreatedAt = DateTime.UtcNow,
//                    PeriodStart = today,
//                    EventType = ProgressEventType.Daily,
//                    LastUpdated = DateTime.UtcNow
//                };

//                await _collection.InsertOneAsync(progress);
//            }

//            return progress;
//        }

//        /// <summary>
//        /// Update progress with new activity data
//        /// </summary>
//        public async Task UpdateProgressWithTracing(string userId, TracingActivityInput input)
//        {
//            var progress = await GetOrCreateDailyProgress(userId);
//            progress.AddTracingActivity(input);

//            var update = Builders<ChildProgress>.Update
//                .Set(x => x.LastUpdated, DateTime.UtcNow)
//                .Set(GetProgressFieldName(input.ActivityType),
//                     GetProgressDictionary(progress, input.ActivityType));

//            await _collection.UpdateOneAsync(
//                x => x.Id == progress.Id,
//                update
//            );
//        }

//        /// <summary>
//        /// Update progress with quiz data
//        /// </summary>
//        public async Task UpdateProgressWithQuiz(string userId, QuizActivityInput input)
//        {
//            var progress = await GetOrCreateDailyProgress(userId);
//            progress.AddQuizActivity(input);

//            var update = Builders<ChildProgress>.Update
//                .Set(x => x.LastUpdated, DateTime.UtcNow)
//                .Set(GetProgressFieldName(input.ActivityType),
//                     GetProgressDictionary(progress, input.ActivityType));

//            await _collection.UpdateOneAsync(
//                x => x.Id == progress.Id,
//                update
//            );
//        }

//        /// <summary>
//        /// Get focused learning recommendations for a child
//        /// </summary>
//        public async Task<FocusedLearningPlan> GetFocusedLearningPlan(string userId, int daysToAnalyze = 7)
//        {
//            var endDate = DateTime.UtcNow.Date;
//            var startDate = endDate.AddDays(-daysToAnalyze);

//            // Get recent progress data
//            var recentProgress = await _collection.Find(x =>
//                x.UserId == userId &&
//                x.EventType == ProgressEventType.Daily &&
//                x.PeriodStart >= startDate &&
//                x.PeriodStart <= endDate
//            ).ToListAsync();

//            // Aggregate data across days
//            var aggregatedAnalysis = AggregateWeaknessAnalysis(recentProgress);

//            // Generate focused learning plan
//            return GenerateFocusedLearningPlan(aggregatedAnalysis, recentProgress);
//        }

//        /// <summary>
//        /// Get parent report for a specific period
//        /// </summary>
//        public async Task<ParentProgressReport> GetParentReport(string userId, ProgressEventType period)
//        {
//            var progress = await GetLatestProgressForPeriod(userId, period);

//            if (progress == null)
//            {
//                // Generate empty report
//                return new ParentProgressReport
//                {
//                    ChildUserId = userId,
//                    ReportDate = DateTime.UtcNow,
//                    ReportPeriod = period,
//                    CategoryProgress = new Dictionary<ECDGameActivityName, CategoryProgressSummary>(),
//                    ActivityPerformance = new Dictionary<string, ActivityPerformanceSummary>(),
//                    TopPriorityItems = new List<FocusItem>(),
//                    ActivityRecommendations = new List<ActivityRecommendation>()
//                };
//            }

//            return progress.GenerateParentReport();
//        }

//        /// <summary>
//        /// Get activity-specific weaknesses
//        /// </summary>
//        public async Task<Dictionary<string, ActivityWeaknessDetails>> GetActivityWeaknesses(
//            string userId, int daysToAnalyze = 7)
//        {
//            var endDate = DateTime.UtcNow.Date;
//            var startDate = endDate.AddDays(-daysToAnalyze);

//            var recentProgress = await _collection.Find(x =>
//                x.UserId == userId &&
//                x.EventType == ProgressEventType.Daily &&
//                x.PeriodStart >= startDate &&
//                x.PeriodStart <= endDate
//            ).ToListAsync();

//            return AnalyzeActivityWeaknesses(recentProgress);
//        }

//        private async Task<ChildProgress> GetLatestProgressForPeriod(string userId, ProgressEventType period)
//        {
//            var query = _collection.Find(x => x.UserId == userId && x.EventType == period);

//            return await query
//                .SortByDescending(x => x.PeriodStart)
//                .FirstOrDefaultAsync();
//        }

//        private WeaknessAnalysis AggregateWeaknessAnalysis(List<ChildProgress> progressList)
//        {
//            var aggregated = new WeaknessAnalysis();
//            var weakItemScores = new Dictionary<string, List<double>>();
//            var activityScores = new Dictionary<string, List<double>>();

//            foreach (var progress in progressList)
//            {
//                var analysis = progress.GetWeaknessAnalysis();

//                // Aggregate weak items
//                foreach (var weakItem in analysis.WeakNumbers)
//                    AddToWeakItemScores(weakItemScores, $"Number:{weakItem.ItemName}", weakItem.OverallScore);

//                foreach (var weakItem in analysis.WeakCapitalAlphabets)
//                    AddToWeakItemScores(weakItemScores, $"Capital:{weakItem.ItemName}", weakItem.OverallScore);

//                foreach (var weakItem in analysis.WeakSmallAlphabets)
//                    AddToWeakItemScores(weakItemScores, $"Small:{weakItem.ItemName}", weakItem.OverallScore);

//                foreach (var weakItem in analysis.WeakShapes)
//                    AddToWeakItemScores(weakItemScores, $"Shape:{weakItem.ItemName}", weakItem.OverallScore);

//                foreach (var weakItem in analysis.WeakColors)
//                    AddToWeakItemScores(weakItemScores, $"Color:{weakItem.ItemName}", weakItem.OverallScore);

//                // Aggregate activity weaknesses
//                foreach (var activity in analysis.WeakestActivities)
//                    AddToWeakItemScores(activityScores, activity.ActivityType, activity.AverageSuccessRate);
//            }

//            // Build final aggregated analysis
//            return BuildAggregatedAnalysis(weakItemScores, activityScores);
//        }

//        private void AddToWeakItemScores(Dictionary<string, List<double>> scores, string key, double score)
//        {
//            if (!scores.ContainsKey(key))
//                scores[key] = new List<double>();
//            scores[key].Add(score);
//        }

//        private WeaknessAnalysis BuildAggregatedAnalysis(
//            Dictionary<string, List<double>> weakItemScores,
//            Dictionary<string, List<double>> activityScores)
//        {
//            var analysis = new WeaknessAnalysis();

//            // Process weak items by category
//            foreach (var kvp in weakItemScores.OrderBy(x => x.Value.Average()))
//            {
//                var parts = kvp.Key.Split(':');
//                var category = parts[0];
//                var itemName = parts[1];
//                var avgScore = kvp.Value.Average();

//                var weakItem = new WeakItem
//                {
//                    Category = category,
//                    ItemName = itemName,
//                    OverallScore = avgScore,
//                    TotalAttempts = kvp.Value.Count
//                };

//                switch (category)
//                {
//                    case "Number":
//                        analysis.WeakNumbers.Add(weakItem);
//                        break;
//                    case "Capital":
//                        analysis.WeakCapitalAlphabets.Add(weakItem);
//                        break;
//                    case "Small":
//                        analysis.WeakSmallAlphabets.Add(weakItem);
//                        break;
//                    case "Shape":
//                        analysis.WeakShapes.Add(weakItem);
//                        break;
//                    case "Color":
//                        analysis.WeakColors.Add(weakItem);
//                        break;
//                }
//            }

//            // Process activity weaknesses
//            foreach (var kvp in activityScores.OrderBy(x => x.Value.Average()))
//            {
//                analysis.WeakestActivities.Add(new ActivityWeakness
//                {
//                    ActivityType = kvp.Key,
//                    AverageSuccessRate = kvp.Value.Average(),
//                    TotalAttempts = kvp.Value.Count
//                });
//            }

//            return analysis;
//        }

//        private FocusedLearningPlan GenerateFocusedLearningPlan(
//            WeaknessAnalysis aggregatedAnalysis,
//            List<ChildProgress> recentProgress)
//        {
//            var plan = new FocusedLearningPlan
//            {
//                GeneratedDate = DateTime.UtcNow,
//                PlanDuration = 7, // 7 days
//                DailyFocusItems = new List<DailyFocus>()
//            };

//            // Get top priority items
//            var priorityItems = aggregatedAnalysis.GetPrioritizedFocusItems(20);

//            // Distribute items across days
//            for (int day = 0; day < 7; day++)
//            {
//                var dailyFocus = new DailyFocus
//                {
//                    Day = day + 1,
//                    Date = DateTime.UtcNow.Date.AddDays(day),
//                    FocusItems = new List<FocusActivity>(),
//                    EstimatedDuration = 0
//                };

//                // Add 3-4 items per day
//                var itemsForDay = priorityItems.Skip(day * 3).Take(3).ToList();

//                foreach (var item in itemsForDay)
//                {
//                    var focusActivity = new FocusActivity
//                    {
//                        Category = item.Category,
//                        ItemName = item.ItemName,
//                        ActivityType = item.WeakestActivity,
//                        TargetScore = 80,
//                        EstimatedMinutes = 5,
//                        Reason = $"Current score: {item.Score:F0}%"
//                    };

//                    dailyFocus.FocusItems.Add(focusActivity);
//                    dailyFocus.EstimatedDuration += focusActivity.EstimatedMinutes;
//                }

//                plan.DailyFocusItems.Add(dailyFocus);
//            }

//            return plan;
//        }

//        private Dictionary<string, ActivityWeaknessDetails> AnalyzeActivityWeaknesses(
//            List<ChildProgress> progressList)
//        {
//            var activityDetails = new Dictionary<string, ActivityWeaknessDetails>();
//            var activityTypes = new[]
//            {
//                "Tracing", "Object Recognition", "Listening", "Text to Figure",
//                "Figure to Text", "Counting", "Bubble Pop"
//            };

//            foreach (var activityType in activityTypes)
//            {
//                var details = new ActivityWeaknessDetails
//                {
//                    ActivityType = activityType,
//                    WeakItemsByCategory = new Dictionary<ECDGameActivityName, List<ItemPerformance>>()
//                };

//                // Analyze each category
//                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Numbers, details);
//                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.CapitalAlphabet, details);
//                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.SmallAlphabet, details);
//                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Shapes, details);
//                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Colors, details);

//                // Calculate overall stats
//                details.CalculateOverallStats();

//                activityDetails[activityType] = details;
//            }

//            return activityDetails;
//        }

//        private void AnalyzeCategoryForActivity(
//            List<ChildProgress> progressList,
//            string activityType,
//            ECDGameActivityName category,
//            ActivityWeaknessDetails details)
//        {
//            var itemPerformances = new Dictionary<string, ItemPerformance>();

//            foreach (var progress in progressList)
//            {
//                var dict = progress.GetProgressDictionary(category);
//                if (dict == null) continue;

//                foreach (var kvp in dict)
//                {
//                    var itemProgress = kvp.Value;
//                    var (attempts, success, time) = progress.GetActivitySpecificStats(itemProgress, activityType);

//                    if (attempts > 0)
//                    {
//                        if (!itemPerformances.ContainsKey(kvp.Key))
//                        {
//                            itemPerformances[kvp.Key] = new ItemPerformance
//                            {
//                                ItemName = kvp.Key,
//                                TotalAttempts = 0,
//                                TotalSuccess = 0,
//                                TotalTime = 0
//                            };
//                        }

//                        var perf = itemPerformances[kvp.Key];
//                        perf.TotalAttempts += attempts;
//                        perf.TotalSuccess += success;
//                        perf.TotalTime += time;
//                    }
//                }
//            }

//            // Calculate success rates and identify weak items
//            var weakItems = itemPerformances.Values
//                .Where(p => p.SuccessRate < 70)
//                .OrderBy(p => p.SuccessRate)
//                .ToList();

//            if (weakItems.Any())
//            {
//                details.WeakItemsByCategory[category] = weakItems;
//            }
//        }

//        private string GetProgressFieldName(ECDGameActivityName activityType)
//        {
//            switch (activityType)
//            {
//                case ECDGameActivityName.Numbers:
//                    return "numberProgress";
//                case ECDGameActivityName.CapitalAlphabet:
//                    return "capitalAlphabetProgress";
//                case ECDGameActivityName.SmallAlphabet:
//                    return "smallAlphabetProgress";
//                case ECDGameActivityName.Shapes:
//                    return "shapeProgress";
//                case ECDGameActivityName.Colors:
//                    return "colorProgress";
//                default:
//                    throw new ArgumentException($"Unknown activity type: {activityType}");
//            }
//        }

//        private Dictionary<string, ECDGamesActivityProgress> GetProgressDictionary(
//            ChildProgress progress, ECDGameActivityName activityType)
//        {
//            switch (activityType)
//            {
//                case ECDGameActivityName.Numbers:
//                    return progress.NumberProgressDict;
//                case ECDGameActivityName.CapitalAlphabet:
//                    return progress.CapitalAlphabetProgressDict;
//                case ECDGameActivityName.SmallAlphabet:
//                    return progress.SmallAlphabetProgressDict;
//                case ECDGameActivityName.Shapes:
//                    return progress.ShapeProgressDict;
//                case ECDGameActivityName.Colors:
//                    return progress.ColorProgressDict;
//                default:
//                    return null;
//            }
//        }
//    }

//    /// <summary>
//    /// Focused learning plan for a child
//    /// </summary>
//    public class FocusedLearningPlan
//    {
//        public DateTime GeneratedDate { get; set; }
//        public int PlanDuration { get; set; } // in days
//        public List<DailyFocus> DailyFocusItems { get; set; }

//        public string GetPlanSummary()
//        {
//            var summary = new System.Text.StringBuilder();

//            summary.AppendLine($"7-Day Learning Plan (Generated: {GeneratedDate:MMM dd})");
//            summary.AppendLine(new string('-', 40));

//            foreach (var day in DailyFocusItems)
//            {
//                summary.AppendLine($"\nDay {day.Day} ({day.Date:MMM dd}):");
//                summary.AppendLine($"Duration: {day.EstimatedDuration} minutes");

//                foreach (var item in day.FocusItems)
//                {
//                    summary.AppendLine($"  • {item.Category} '{item.ItemName}' - {item.ActivityType}");
//                    summary.AppendLine($"    Goal: Reach {item.TargetScore}% ({item.Reason})");
//                }
//            }

//            return summary.ToString();
//        }
//    }

//    /// <summary>
//    /// Daily focus items
//    /// </summary>
//    public class DailyFocus
//    {
//        public int Day { get; set; }
//        public DateTime Date { get; set; }
//        public List<FocusActivity> FocusItems { get; set; }
//        public int EstimatedDuration { get; set; } // in minutes
//    }

//    /// <summary>
//    /// Specific activity to focus on
//    /// </summary>
//    public class FocusActivity
//    {
//        public ECDGameActivityName Category { get; set; }
//        public string ItemName { get; set; }
//        public string ActivityType { get; set; }
//        public int TargetScore { get; set; }
//        public int EstimatedMinutes { get; set; }
//        public string Reason { get; set; }
//    }

//    /// <summary>
//    /// Detailed weakness analysis for activities
//    /// </summary>
//    public class ActivityWeaknessDetails
//    {
//        public string ActivityType { get; set; }
//        public Dictionary<ECDGameActivityName, List<ItemPerformance>> WeakItemsByCategory { get; set; }
//        public double OverallSuccessRate { get; set; }
//        public int TotalAttempts { get; set; }
//        public List<string> TopWeakItems { get; set; } = new List<string>();

//        public void CalculateOverallStats()
//        {
//            int totalAttempts = 0;
//            int totalSuccess = 0;

//            foreach (var category in WeakItemsByCategory)
//            {
//                foreach (var item in category.Value)
//                {
//                    totalAttempts += item.TotalAttempts;
//                    totalSuccess += item.TotalSuccess;

//                    if (item.SuccessRate < 50)
//                    {
//                        TopWeakItems.Add($"{category.Key}:{item.ItemName}");
//                    }
//                }
//            }

//            TotalAttempts = totalAttempts;
//            OverallSuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
//        }
//    }

//    /// <summary>
//    /// Item performance metrics
//    /// </summary>
//    public class ItemPerformance
//    {
//        public string ItemName { get; set; }
//        public int TotalAttempts { get; set; }
//        public int TotalSuccess { get; set; }
//        public double TotalTime { get; set; }
//        public double SuccessRate => TotalAttempts > 0 ? (double)TotalSuccess / TotalAttempts * 100 : 0;
//        public double AverageTime => TotalAttempts > 0 ? TotalTime / TotalAttempts : 0;
//    }
//}