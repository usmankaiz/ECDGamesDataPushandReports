using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NumberLandStructure.Data;
using NumberLandStructure.Input;
using NumberLandStructure.Logic;
using NumberLandStructure.Tracking;

namespace NumberLandStructure.Repository
{
    /// <summary>
    /// MongoDB repository for child progress data with tracking capabilities
    /// </summary>
    public class ChildProgressRepository
    {
        private readonly IMongoCollection<ChildProgress> _collection;
        private readonly ProgressLogic _progressLogic;
        private readonly AnalysisLogic _analysisLogic;
        private readonly RecommendationLogic _recommendationLogic;
        private readonly ValidationLogic _validationLogic;
        private readonly ProgressTracker _progressTracker;

        public ChildProgressRepository(IMongoDatabase database)
        {
            _collection = database.GetCollection<ChildProgress>("child_progress");
            _progressLogic = new ProgressLogic();
            _analysisLogic = new AnalysisLogic();
            _recommendationLogic = new RecommendationLogic();
            _validationLogic = new ValidationLogic();
            _progressTracker = new ProgressTracker();
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

        #region NEW TRACKING METHODS

        /// <summary>
        /// Get complete progress overview for a child
        /// </summary>
        public async Task<ProgressOverview> GetProgressOverviewAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);

            if (progress == null)
            {
                return new ProgressOverview
                {
                    UserId = userId,
                    GeneratedDate = DateTime.UtcNow,
                    CategoryProgress = new Dictionary<ECDGameActivityName, CategoryTracking>()
                };
            }

            return _progressTracker.GetProgressOverview(progress);
        }

        /// <summary>
        /// Get detailed progress for a specific category
        /// </summary>
        public async Task<CategoryDetailView> GetCategoryProgressAsync(string userId, ECDGameActivityName category)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);

            if (progress == null)
            {
                return new CategoryDetailView
                {
                    Category = category,
                    ExpectedItems = GetExpectedItemsForCategory(category),
                    ItemDetails = new List<ItemDetailView>()
                };
            }

            return _progressTracker.GetCategoryDetails(progress, category);
        }

        /// <summary>
        /// Get completion summary across all categories
        /// </summary>
        public async Task<CompletionSummary> GetCompletionSummaryAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);

            if (progress == null)
            {
                return new CompletionSummary
                {
                    UserId = userId,
                    GeneratedDate = DateTime.UtcNow,
                    CategoryCompletions = new Dictionary<ECDGameActivityName, CategoryCompletion>()
                };
            }

            return _progressTracker.GetCompletionSummary(progress);
        }

        /// <summary>
        /// Get progress for a specific activity type across all items
        /// </summary>
        public async Task<ActivityProgressView> GetActivityProgressAsync(string userId, string activityType)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);

            if (progress == null)
            {
                return new ActivityProgressView
                {
                    ActivityType = activityType,
                    GeneratedDate = DateTime.UtcNow,
                    CategoryResults = new Dictionary<ECDGameActivityName, List<ItemActivityResult>>()
                };
            }

            return _progressTracker.GetActivityProgress(progress, activityType);
        }

        /// <summary>
        /// Get list of completed items across all categories
        /// </summary>
        public async Task<List<CompletedItem>> GetCompletedItemsAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);
            var completedItems = new List<CompletedItem>();

            if (progress == null) return completedItems;

            var categoryDictionaries = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            foreach (var (category, dict) in categoryDictionaries)
            {
                if (dict != null)
                {
                    foreach (var kvp in dict.Where(p => p.Value.Completed))
                    {
                        completedItems.Add(new CompletedItem
                        {
                            Category = category,
                            ItemName = kvp.Key,
                            CompletedDate = progress.LastUpdated,
                            OverallScore = _analysisLogic.CalculateOverallScore(kvp.Value),
                            TotalAttempts = _analysisLogic.GetTotalAttempts(kvp.Value),
                            TotalTimeSpent = _analysisLogic.GetTotalTimeSpent(kvp.Value)
                        });
                    }
                }
            }

            return completedItems.OrderByDescending(item => item.CompletedDate).ToList();
        }

        /// <summary>
        /// Get list of items in progress (attempted but not completed)
        /// </summary>
        public async Task<List<InProgressItem>> GetInProgressItemsAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);
            var inProgressItems = new List<InProgressItem>();

            if (progress == null) return inProgressItems;

            var categoryDictionaries = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            foreach (var (category, dict) in categoryDictionaries)
            {
                if (dict != null)
                {
                    foreach (var kvp in dict.Where(p => !p.Value.Completed))
                    {
                        var itemProgress = kvp.Value;
                        inProgressItems.Add(new InProgressItem
                        {
                            Category = category,
                            ItemName = kvp.Key,
                            LastAttempted = progress.LastUpdated,
                            CurrentScore = _analysisLogic.CalculateOverallScore(itemProgress),
                            TotalAttempts = _analysisLogic.GetTotalAttempts(itemProgress),
                            TotalTimeSpent = _analysisLogic.GetTotalTimeSpent(itemProgress),
                            WeakestActivity = _analysisLogic.GetWeakestActivity(itemProgress),
                            CompletionRate = _analysisLogic.GetCompletionRate(itemProgress)
                        });
                    }
                }
            }

            return inProgressItems.OrderBy(item => item.CurrentScore).ToList();
        }

        /// <summary>
        /// Get items that haven't been attempted yet
        /// </summary>
        public async Task<List<NotStartedItem>> GetNotStartedItemsAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);
            var notStartedItems = new List<NotStartedItem>();

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
                var expectedItems = GetExpectedItemsForCategory(category);
                var progressDict = progress != null ? _progressLogic.GetProgressDictionary(progress, category) : null;

                foreach (var expectedItem in expectedItems)
                {
                    if (progressDict?.ContainsKey(expectedItem) != true)
                    {
                        notStartedItems.Add(new NotStartedItem
                        {
                            Category = category,
                            ItemName = expectedItem,
                            RecommendedOrder = GetRecommendedOrder(category, expectedItem)
                        });
                    }
                }
            }

            return notStartedItems.OrderBy(item => item.RecommendedOrder).ToList();
        }

        /// <summary>
        /// Get items that need immediate attention (low scores, multiple failures)
        /// </summary>
        public async Task<List<AttentionItem>> GetItemsNeedingAttentionAsync(string userId)
        {
            var progress = await GetProgressAsync(userId, ProgressEventType.Daily);
            var attentionItems = new List<AttentionItem>();

            if (progress == null) return attentionItems;

            var categoryDictionaries = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            foreach (var (category, dict) in categoryDictionaries)
            {
                if (dict != null)
                {
                    foreach (var kvp in dict)
                    {
                        var itemProgress = kvp.Value;
                        var totalAttempts = _analysisLogic.GetTotalAttempts(itemProgress);
                        var overallScore = _analysisLogic.CalculateOverallScore(itemProgress);

                        // Items that need attention: attempted multiple times but low score
                        if (totalAttempts >= 3 && (overallScore < 70 || !itemProgress.Completed))
                        {
                            var reasons = new List<string>();

                            if (overallScore < 50)
                                reasons.Add("Very low score");
                            else if (overallScore < 70)
                                reasons.Add("Below average score");

                            if (itemProgress.QuizFailCount > itemProgress.QuizCount / 2)
                                reasons.Add("High quiz failure rate");

                            if (itemProgress.TracingCount > 0 && itemProgress.TracingCompleteCount == 0)
                                reasons.Add("Unable to complete tracing");

                            if (!itemProgress.Completed && totalAttempts >= 5)
                                reasons.Add("Many attempts without completion");

                            attentionItems.Add(new AttentionItem
                            {
                                Category = category,
                                ItemName = kvp.Key,
                                CurrentScore = overallScore,
                                TotalAttempts = totalAttempts,
                                LastAttempted = progress.LastUpdated,
                                AttentionReasons = reasons,
                                RecommendedAction = GetRecommendedAction(itemProgress, overallScore),
                                PriorityLevel = CalculatePriorityLevel(overallScore, totalAttempts)
                            });
                        }
                    }
                }
            }

            return attentionItems.OrderByDescending(item => item.PriorityLevel)
                                 .ThenBy(item => item.CurrentScore)
                                 .ToList();
        }

        /// <summary>
        /// Get progress statistics for a specific time period
        /// </summary>
        public async Task<ProgressStatistics> GetProgressStatisticsAsync(string userId, int days = 7)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days);

            var progressRecords = await GetProgressRangeAsync(userId, startDate, endDate);

            var stats = new ProgressStatistics
            {
                UserId = userId,
                PeriodStart = startDate,
                PeriodEnd = endDate,
                DaysAnalyzed = days,
                GeneratedDate = DateTime.UtcNow
            };

            if (!progressRecords.Any())
                return stats;

            // Calculate statistics across all records
            var allItems = new List<ECDGamesActivityProgress>();

            foreach (var record in progressRecords)
            {
                var allDicts = new[]
                {
                    record.NumberProgressDict,
                    record.CapitalAlphabetProgressDict,
                    record.SmallAlphabetProgressDict,
                    record.ShapeProgressDict,
                    record.ColorProgressDict
                };

                foreach (var dict in allDicts.Where(d => d != null))
                {
                    allItems.AddRange(dict.Values);
                }
            }

            if (allItems.Any())
            {
                stats.TotalItemsAttempted = allItems.Count;
                stats.TotalItemsCompleted = allItems.Count(item => item.Completed);
                stats.TotalAttempts = allItems.Sum(item => _analysisLogic.GetTotalAttempts(item));
                stats.TotalTimeSpent = allItems.Sum(item => _analysisLogic.GetTotalTimeSpent(item));
                stats.AverageScore = allItems.Average(item => _analysisLogic.CalculateOverallScore(item));
                stats.CompletionRate = (double)stats.TotalItemsCompleted / stats.TotalItemsAttempted * 100;

                // Calculate activity-specific statistics
                stats.TracingAttempts = allItems.Sum(item => item.TracingCount);
                stats.TracingCompletions = allItems.Sum(item => item.TracingCompleteCount);
                stats.QuizAttempts = allItems.Sum(item => item.QuizCount);
                stats.QuizFailures = allItems.Sum(item => item.QuizFailCount);

                stats.TracingSuccessRate = stats.TracingAttempts > 0 ?
                    (double)stats.TracingCompletions / stats.TracingAttempts * 100 : 0;
                stats.QuizSuccessRate = stats.QuizAttempts > 0 ?
                    (double)(stats.QuizAttempts - stats.QuizFailures) / stats.QuizAttempts * 100 : 0;
            }

            return stats;
        }

        #endregion

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

        private int GetRecommendedOrder(ECDGameActivityName category, string itemName)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers:
                    return int.TryParse(itemName, out int number) ? number : 999;

                case ECDGameActivityName.CapitalAlphabet:
                case ECDGameActivityName.SmallAlphabet:
                    return itemName.Length > 0 ? itemName[0] : 999;

                default:
                    var expectedItems = GetExpectedItemsForCategory(category);
                    return expectedItems.IndexOf(itemName) + 1;
            }
        }

        private string GetRecommendedAction(ECDGamesActivityProgress itemProgress, double overallScore)
        {
            if (overallScore < 30)
                return "Start with basic tracing practice";

            if (overallScore < 50)
                return "Focus on fundamental activities before quizzes";

            if (overallScore < 70)
            {
                var weakestActivity = _analysisLogic.GetWeakestActivity(itemProgress);
                return $"Practice {weakestActivity} activities";
            }

            return "Continue regular practice to maintain proficiency";
        }

        private int CalculatePriorityLevel(double score, int attempts)
        {
            int priority = 1; // Base priority

            if (score < 30) priority += 3;
            else if (score < 50) priority += 2;
            else if (score < 70) priority += 1;

            if (attempts >= 10) priority += 2;
            else if (attempts >= 5) priority += 1;

            return Math.Min(priority, 5); // Max priority level of 5
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

    #region NEW TRACKING DATA MODELS

    /// <summary>
    /// Represents a completed item
    /// </summary>
    public class CompletedItem
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public DateTime CompletedDate { get; set; }
        public double OverallScore { get; set; }
        public int TotalAttempts { get; set; }
        public double TotalTimeSpent { get; set; }
    }

    /// <summary>
    /// Represents an item in progress
    /// </summary>
    public class InProgressItem
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public DateTime LastAttempted { get; set; }
        public double CurrentScore { get; set; }
        public int TotalAttempts { get; set; }
        public double TotalTimeSpent { get; set; }
        public string WeakestActivity { get; set; }
        public double CompletionRate { get; set; }
    }

    /// <summary>
    /// Represents an item not yet started
    /// </summary>
    public class NotStartedItem
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public int RecommendedOrder { get; set; }
    }

    /// <summary>
    /// Represents an item needing attention
    /// </summary>
    public class AttentionItem
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public double CurrentScore { get; set; }
        public int TotalAttempts { get; set; }
        public DateTime LastAttempted { get; set; }
        public List<string> AttentionReasons { get; set; } = new List<string>();
        public string RecommendedAction { get; set; }
        public int PriorityLevel { get; set; } // 1-5, 5 being highest priority
    }

    /// <summary>
    /// Progress statistics for a time period
    /// </summary>
    public class ProgressStatistics
    {
        public string UserId { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public int DaysAnalyzed { get; set; }
        public DateTime GeneratedDate { get; set; }

        // Overall statistics
        public int TotalItemsAttempted { get; set; }
        public int TotalItemsCompleted { get; set; }
        public int TotalAttempts { get; set; }
        public double TotalTimeSpent { get; set; }
        public double AverageScore { get; set; }
        public double CompletionRate { get; set; }

        // Activity-specific statistics
        public int TracingAttempts { get; set; }
        public int TracingCompletions { get; set; }
        public double TracingSuccessRate { get; set; }
        public int QuizAttempts { get; set; }
        public int QuizFailures { get; set; }
        public double QuizSuccessRate { get; set; }
    }

    #endregion

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