using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NumberLandStructure.Data;
using NumberLandStructure.Input;
using NumberLandStructure.Logic;

namespace NumberLandStructure.Repository
{
    /// <summary>
    /// MongoDB repository for child progress data
    /// </summary>
    public class ChildProgressRepository
    {
        private readonly IMongoCollection<ChildProgress> _collection;
        private readonly ProgressLogic _progressLogic;
        private readonly AnalysisLogic _analysisLogic;
        private readonly RecommendationLogic _recommendationLogic;
        private readonly ValidationLogic _validationLogic;

        public ChildProgressRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<ChildProgress>("child_progress");
            _progressLogic = new ProgressLogic();
            _analysisLogic = new AnalysisLogic();
            _recommendationLogic = new RecommendationLogic();
            _validationLogic = new ValidationLogic();
            CreateIndexes();
        }

        private void CreateIndexes()
        {
            // Compound index for efficient queries
            _collection.Indexes.CreateOne(new CreateIndexModel<ChildProgress>(
                Builders<ChildProgress>.IndexKeys
                    .Ascending(x => x.UserId)
                    .Descending(x => x.PeriodStart)
                    .Ascending(x => x.EventType)
            ));

            // Index for last updated queries
            _collection.Indexes.CreateOne(new CreateIndexModel<ChildProgress>(
                Builders<ChildProgress>.IndexKeys.Descending(x => x.LastUpdated)
            ));
        }

        /// <summary>
        /// Get or create progress record for today
        /// </summary>
        public async Task<ChildProgress> GetOrCreateDailyProgressAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;

            var progress = await _collection.Find(x =>
                x.UserId == userId &&
                x.EventType == ProgressEventType.Daily &&
                x.PeriodStart == today
            ).FirstOrDefaultAsync();

            if (progress == null)
            {
                progress = new ChildProgress
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    PeriodStart = today,
                    EventType = ProgressEventType.Daily,
                    LastUpdated = DateTime.UtcNow
                };

                await _collection.InsertOneAsync(progress);
            }

            return progress;
        }

        /// <summary>
        /// Add tracing activity to child's progress
        /// </summary>
        public async Task<OperationResult> AddTracingActivityAsync(string userId, TracingActivityInput input)
        {
            try
            {
                // Validate input
                var validation = _validationLogic.ValidateTracingInput(input);
                if (!validation.IsValid)
                {
                    return OperationResult.Failure(string.Join("; ", validation.Errors));
                }

                // Get or create progress record
                var progress = await GetOrCreateDailyProgressAsync(userId);

                // Process the tracing activity
                _progressLogic.ProcessTracingActivity(progress, input);

                // Update in database
                var update = Builders<ChildProgress>.Update
                    .Set(x => x.LastUpdated, DateTime.UtcNow)
                    .Set(GetProgressFieldName(input.ActivityType),
                         _progressLogic.GetProgressDictionary(progress, input.ActivityType));

                await _collection.UpdateOneAsync(
                    x => x.Id == progress.Id,
                    update
                );

                return OperationResult.Success("Tracing activity added successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Error adding tracing activity: {ex.Message}");
            }
        }

        /// <summary>
        /// Add quiz activity to child's progress
        /// </summary>
        public async Task<OperationResult> AddQuizActivityAsync(string userId, QuizActivityInput input)
        {
            try
            {
                // Validate input
                var validation = _validationLogic.ValidateQuizInput(input);
                if (!validation.IsValid)
                {
                    return OperationResult.Failure(string.Join("; ", validation.Errors));
                }

                // Get or create progress record
                var progress = await GetOrCreateDailyProgressAsync(userId);

                // Process the quiz activity
                _progressLogic.ProcessQuizActivity(progress, input);

                // Update in database
                var update = Builders<ChildProgress>.Update
                    .Set(x => x.LastUpdated, DateTime.UtcNow)
                    .Set(GetProgressFieldName(input.ActivityType),
                         _progressLogic.GetProgressDictionary(progress, input.ActivityType));

                await _collection.UpdateOneAsync(
                    x => x.Id == progress.Id,
                    update
                );

                return OperationResult.Success("Quiz activity added successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Error adding quiz activity: {ex.Message}");
            }
        }

        /// <summary>
        /// Add batch activities to child's progress
        /// </summary>
        public async Task<OperationResult> AddBatchActivitiesAsync(BatchActivityInput batchInput)
        {
            try
            {
                // Validate batch input
                var validation = _validationLogic.ValidateBatchInput(batchInput);
                if (!validation.IsValid)
                {
                    return OperationResult.Failure(string.Join("; ", validation.Errors));
                }

                var results = new List<string>();

                // Process tracing activities
                foreach (var tracingInput in batchInput.TracingActivities)
                {
                    var result = await AddTracingActivityAsync(batchInput.UserId, tracingInput);
                    if (!result.IsSuccess)
                    {
                        results.Add($"Tracing {tracingInput.ItemDetails}: {result.Message}");
                    }
                }

                // Process quiz activities
                foreach (var quizInput in batchInput.QuizActivities)
                {
                    var result = await AddQuizActivityAsync(batchInput.UserId, quizInput);
                    if (!result.IsSuccess)
                    {
                        results.Add($"Quiz {quizInput.ItemDetails}: {result.Message}");
                    }
                }

                if (results.Any())
                {
                    return OperationResult.Failure($"Some activities failed: {string.Join("; ", results)}");
                }

                return OperationResult.Success("All batch activities added successfully");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Error processing batch activities: {ex.Message}");
            }
        }

        /// <summary>
        /// Get child's progress for a specific period
        /// </summary>
        public async Task<ChildProgress> GetProgressAsync(string userId, ProgressEventType eventType, DateTime? periodStart = null)
        {
            var query = _collection.Find(x => x.UserId == userId && x.EventType == eventType);

            if (periodStart.HasValue)
            {
                query = query.Where(x => x.PeriodStart == periodStart.Value);
            }

            return await query
                .SortByDescending(x => x.PeriodStart)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get multiple progress records for analysis
        /// </summary>
        public async Task<List<ChildProgress>> GetProgressRangeAsync(string userId, DateTime startDate, DateTime endDate, ProgressEventType eventType = ProgressEventType.Daily)
        {
            return await _collection.Find(x =>
                x.UserId == userId &&
                x.EventType == eventType &&
                x.PeriodStart >= startDate &&
                x.PeriodStart <= endDate
            ).ToListAsync();
        }

        /// <summary>
        /// Get weakness analysis for a child
        /// </summary>
        public async Task<WeaknessAnalysis> GetWeaknessAnalysisAsync(string userId, int daysToAnalyze = 7)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-daysToAnalyze);

            var recentProgress = await GetProgressRangeAsync(userId, startDate, endDate);

            if (!recentProgress.Any())
            {
                return new WeaknessAnalysis();
            }

            // If single day, analyze that day directly
            if (recentProgress.Count == 1)
            {
                return _analysisLogic.AnalyzeWeaknesses(recentProgress.First());
            }

            // For multiple days, aggregate the analysis
            return AggregateWeaknessAnalysis(recentProgress);
        }

        /// <summary>
        /// Get focused learning plan for a child
        /// </summary>
        public async Task<FocusedLearningPlan> GetFocusedLearningPlanAsync(string userId, int daysToAnalyze = 7, int planDuration = 7)
        {
            var weaknessAnalysis = await GetWeaknessAnalysisAsync(userId, daysToAnalyze);
            return _recommendationLogic.GenerateLearningPlan(weaknessAnalysis, planDuration);
        }

        /// <summary>
        /// Get activity recommendations for a child
        /// </summary>
        public async Task<List<ActivityRecommendation>> GetActivityRecommendationsAsync(string userId, int daysToAnalyze = 7)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);
            if (progress == null)
            {
                return new List<ActivityRecommendation>();
            }

            var weaknessAnalysis = await GetWeaknessAnalysisAsync(userId, daysToAnalyze);
            return _recommendationLogic.GenerateRecommendations(progress, weaknessAnalysis);
        }

        /// <summary>
        /// Get detailed activity weakness analysis
        /// </summary>
        public async Task<Dictionary<string, ActivityWeaknessDetails>> GetActivityWeaknessDetailsAsync(string userId, int daysToAnalyze = 7)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-daysToAnalyze);

            var recentProgress = await GetProgressRangeAsync(userId, startDate, endDate);

            return AnalyzeActivityWeaknesses(recentProgress);
        }

        /// <summary>
        /// Delete child's progress data
        /// </summary>
        public async Task<OperationResult> DeleteChildProgressAsync(string userId)
        {
            try
            {
                var result = await _collection.DeleteManyAsync(x => x.UserId == userId);
                return OperationResult.Success($"Deleted {result.DeletedCount} progress records");
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Error deleting progress: {ex.Message}");
            }
        }

        /// <summary>
        /// Get child's overall statistics
        /// </summary>
        public async Task<ChildStatistics> GetChildStatisticsAsync(string userId, int daysToAnalyze = 30)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-daysToAnalyze);

            var progressRecords = await GetProgressRangeAsync(userId, startDate, endDate);

            var stats = new ChildStatistics
            {
                UserId = userId,
                AnalysisPeriod = daysToAnalyze,
                TotalDaysActive = progressRecords.Count,
                GeneratedDate = DateTime.UtcNow
            };

            if (!progressRecords.Any())
            {
                return stats;
            }

            // Calculate aggregate statistics
            foreach (var progress in progressRecords)
            {
                var allDicts = new[]
                {
                    progress.NumberProgressDict,
                    progress.CapitalAlphabetProgressDict,
                    progress.SmallAlphabetProgressDict,
                    progress.ShapeProgressDict,
                    progress.ColorProgressDict
                };

                foreach (var dict in allDicts.Where(d => d != null))
                {
                    foreach (var itemProgress in dict.Values)
                    {
                        stats.TotalActivitiesAttempted += _analysisLogic.GetTotalAttempts(itemProgress);
                        stats.TotalTimeSpent += _analysisLogic.GetTotalTimeSpent(itemProgress);

                        if (itemProgress.Completed)
                            stats.TotalActivitiesCompleted++;

                        // Count successful activities
                        if (itemProgress.TracingCount > 0)
                            stats.TotalActivitiesCompleted += itemProgress.TracingCompleteCount;

                        if (itemProgress.QuizCount > 0)
                            stats.TotalActivitiesCompleted += itemProgress.QuizCount - itemProgress.QuizFailCount;
                    }
                }
            }

            // Calculate success rate
            stats.OverallSuccessRate = stats.TotalActivitiesAttempted > 0
                ? (double)stats.TotalActivitiesCompleted / stats.TotalActivitiesAttempted * 100
                : 0;

            return stats;
        }

        #region Private Helper Methods

        private string GetProgressFieldName(ECDGameActivityName activityType)
        {
            switch (activityType)
            {
                case ECDGameActivityName.Numbers:
                    return "numberProgress";
                case ECDGameActivityName.CapitalAlphabet:
                    return "capitalAlphabetProgress";
                case ECDGameActivityName.SmallAlphabet:
                    return "smallAlphabetProgress";
                case ECDGameActivityName.Shapes:
                    return "shapeProgress";
                case ECDGameActivityName.Colors:
                    return "colorProgress";
                default:
                    throw new ArgumentException($"Unknown activity type: {activityType}");
            }
        }

        private WeaknessAnalysis AggregateWeaknessAnalysis(List<ChildProgress> progressList)
        {
            var aggregated = new WeaknessAnalysis();
            var weakItemScores = new Dictionary<string, List<double>>();
            var activityScores = new Dictionary<string, List<double>>();

            foreach (var progress in progressList)
            {
                var analysis = _analysisLogic.AnalyzeWeaknesses(progress);

                // Aggregate weak items
                foreach (var weakItem in analysis.WeakNumbers)
                    AddToWeakItemScores(weakItemScores, $"Number:{weakItem.ItemName}", weakItem.OverallScore);

                foreach (var weakItem in analysis.WeakCapitalAlphabets)
                    AddToWeakItemScores(weakItemScores, $"Capital:{weakItem.ItemName}", weakItem.OverallScore);

                foreach (var weakItem in analysis.WeakSmallAlphabets)
                    AddToWeakItemScores(weakItemScores, $"Small:{weakItem.ItemName}", weakItem.OverallScore);

                foreach (var weakItem in analysis.WeakShapes)
                    AddToWeakItemScores(weakItemScores, $"Shape:{weakItem.ItemName}", weakItem.OverallScore);

                foreach (var weakItem in analysis.WeakColors)
                    AddToWeakItemScores(weakItemScores, $"Color:{weakItem.ItemName}", weakItem.OverallScore);

                // Aggregate activity weaknesses
                foreach (var activity in analysis.WeakestActivities)
                    AddToWeakItemScores(activityScores, activity.ActivityType, activity.AverageSuccessRate);
            }

            // Build final aggregated analysis
            return BuildAggregatedAnalysis(weakItemScores, activityScores);
        }

        private void AddToWeakItemScores(Dictionary<string, List<double>> scores, string key, double score)
        {
            if (!scores.ContainsKey(key))
                scores[key] = new List<double>();
            scores[key].Add(score);
        }

        private WeaknessAnalysis BuildAggregatedAnalysis(
            Dictionary<string, List<double>> weakItemScores,
            Dictionary<string, List<double>> activityScores)
        {
            var analysis = new WeaknessAnalysis();

            // Process weak items by category
            foreach (var kvp in weakItemScores.OrderBy(x => x.Value.Average()))
            {
                var parts = kvp.Key.Split(':');
                var category = parts[0];
                var itemName = parts[1];
                var avgScore = kvp.Value.Average();

                var weakItem = new WeakItem
                {
                    Category = category,
                    ItemName = itemName,
                    OverallScore = avgScore,
                    TotalAttempts = kvp.Value.Count
                };

                switch (category)
                {
                    case "Number":
                        analysis.WeakNumbers.Add(weakItem);
                        break;
                    case "Capital":
                        analysis.WeakCapitalAlphabets.Add(weakItem);
                        break;
                    case "Small":
                        analysis.WeakSmallAlphabets.Add(weakItem);
                        break;
                    case "Shape":
                        analysis.WeakShapes.Add(weakItem);
                        break;
                    case "Color":
                        analysis.WeakColors.Add(weakItem);
                        break;
                }
            }

            // Process activity weaknesses
            foreach (var kvp in activityScores.OrderBy(x => x.Value.Average()))
            {
                analysis.WeakestActivities.Add(new ActivityWeakness
                {
                    ActivityType = kvp.Key,
                    AverageSuccessRate = kvp.Value.Average(),
                    TotalAttempts = kvp.Value.Count
                });
            }

            return analysis;
        }

        private Dictionary<string, ActivityWeaknessDetails> AnalyzeActivityWeaknesses(
            List<ChildProgress> progressList)
        {
            var activityDetails = new Dictionary<string, ActivityWeaknessDetails>();
            var activityTypes = new[]
            {
                "Tracing", "Object Recognition", "Listening", "Text to Figure",
                "Figure to Text", "Counting", "Bubble Pop"
            };

            foreach (var activityType in activityTypes)
            {
                var details = new ActivityWeaknessDetails
                {
                    ActivityType = activityType,
                    WeakItemsByCategory = new Dictionary<ECDGameActivityName, List<ItemPerformance>>()
                };

                // Analyze each category
                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Numbers, details);
                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.CapitalAlphabet, details);
                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.SmallAlphabet, details);
                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Shapes, details);
                AnalyzeCategoryForActivity(progressList, activityType, ECDGameActivityName.Colors, details);

                // Calculate overall stats
                details.CalculateOverallStats();

                activityDetails[activityType] = details;
            }

            return activityDetails;
        }

        private void AnalyzeCategoryForActivity(
            List<ChildProgress> progressList,
            string activityType,
            ECDGameActivityName category,
            ActivityWeaknessDetails details)
        {
            var itemPerformances = new Dictionary<string, ItemPerformance>();

            foreach (var progress in progressList)
            {
                var dict = _progressLogic.GetProgressDictionary(progress, category);
                if (dict == null) continue;

                foreach (var kvp in dict)
                {
                    var itemProgress = kvp.Value;
                    var (attempts, success, time) = GetActivitySpecificStats(itemProgress, activityType);

                    if (attempts > 0)
                    {
                        if (!itemPerformances.ContainsKey(kvp.Key))
                        {
                            itemPerformances[kvp.Key] = new ItemPerformance
                            {
                                ItemName = kvp.Key,
                                TotalAttempts = 0,
                                TotalSuccess = 0,
                                TotalTime = 0
                            };
                        }

                        var perf = itemPerformances[kvp.Key];
                        perf.TotalAttempts += attempts;
                        perf.TotalSuccess += success;
                        perf.TotalTime += time;
                    }
                }
            }

            // Calculate success rates and identify weak items
            var weakItems = itemPerformances.Values
                .Where(p => p.SuccessRate < 70)
                .OrderBy(p => p.SuccessRate)
                .ToList();

            if (weakItems.Any())
            {
                details.WeakItemsByCategory[category] = weakItems;
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

    /// <summary>
    /// Operation result for repository operations
    /// </summary>
    public class OperationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public static OperationResult Success(string message = "Operation completed successfully")
        {
            return new OperationResult { IsSuccess = true, Message = message };
        }

        public static OperationResult Failure(string message, Exception exception = null)
        {
            return new OperationResult { IsSuccess = false, Message = message, Exception = exception };
        }
    }

    /// <summary>
    /// Child statistics summary
    /// </summary>
    public class ChildStatistics
    {
        public string UserId { get; set; }
        public DateTime GeneratedDate { get; set; }
        public int AnalysisPeriod { get; set; } // days
        public int TotalDaysActive { get; set; }
        public int TotalActivitiesAttempted { get; set; }
        public int TotalActivitiesCompleted { get; set; }
        public double TotalTimeSpent { get; set; } // seconds
        public double OverallSuccessRate { get; set; } // percentage
        public double AverageSessionTime => TotalDaysActive > 0 ? TotalTimeSpent / TotalDaysActive : 0;
    }

    /// <summary>
    /// Extension methods for ActivityWeaknessDetails
    /// </summary>
    public static class ActivityWeaknessDetailsExtensions
    {
        public static void CalculateOverallStats(this ActivityWeaknessDetails details)
        {
            int totalAttempts = 0;
            int totalSuccess = 0;

            foreach (var category in details.WeakItemsByCategory)
            {
                foreach (var item in category.Value)
                {
                    totalAttempts += item.TotalAttempts;
                    totalSuccess += item.TotalSuccess;

                    if (item.SuccessRate < 50)
                    {
                        details.TopWeakItems.Add($"{category.Key}:{item.ItemName}");
                    }
                }
            }

            details.TotalAttempts = totalAttempts;
            details.OverallSuccessRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
        }
    }
}