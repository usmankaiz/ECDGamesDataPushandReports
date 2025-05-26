using System;
using System.Collections.Generic;
using System.Linq;
using NumberLandStructure.Data;
using NumberLandStructure.Input;

namespace NumberLandStructure.Logic
{
    /// <summary>
    /// Core business logic for progress tracking
    /// </summary>
    public class ProgressLogic
    {
        /// <summary>
        /// Process tracing activity and update progress
        /// </summary>
        public void ProcessTracingActivity(ChildProgress progress, TracingActivityInput input)
        {
            var progressDict = GetProgressDictionary(progress, input.ActivityType);
            if (progressDict == null) return;

            if (!progressDict.ContainsKey(input.ItemDetails))
            {
                progressDict[input.ItemDetails] = new ECDGamesActivityProgress
                {
                    ItemName = input.ItemDetails
                };
            }

            var itemProgress = progressDict[input.ItemDetails];
            itemProgress.TracingCount++;

            if (input.Completed)
            {
                itemProgress.TracingCompleteCount++;
                itemProgress.Completed = true;
            }

            itemProgress.TracingTotalTime += input.TotalTime;
            itemProgress.TotalStars += input.MaxStars;
            itemProgress.TotalStarsAchieved += input.StarsAchieved;

            progress.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Process quiz activity and update progress
        /// </summary>
        public void ProcessQuizActivity(ChildProgress progress, QuizActivityInput input)
        {
            var progressDict = GetProgressDictionary(progress, input.ActivityType);
            if (progressDict == null) return;

            if (!progressDict.ContainsKey(input.ItemDetails))
            {
                progressDict[input.ItemDetails] = new ECDGamesActivityProgress
                {
                    ItemName = input.ItemDetails
                };
            }

            var itemProgress = progressDict[input.ItemDetails];

            foreach (var quiz in input.QuizDetails)
            {
                var activityStats = GetActivityStats(itemProgress, quiz.Type);
                if (activityStats == null) continue;

                activityStats.Count++;
                activityStats.TotalTime += quiz.TimeTaken;

                bool failed = quiz.IsFailed();

                if (failed)
                {
                    activityStats.FailCount++;
                    activityStats.TotalFailTime += quiz.TimeTaken;
                }

                if (quiz.QuizTimeOut)
                {
                    activityStats.QuizTimeOutCount++;
                }

                // Update generic quiz stats
                itemProgress.QuizCount++;
                itemProgress.QuizTotalTime += quiz.TimeTaken;

                if (failed)
                {
                    itemProgress.QuizFailCount++;
                }

                if (quiz.QuizTimeOut)
                {
                    itemProgress.QuizTimeOutCount++;
                }
            }

            // Mark as completed if all quizzes passed with good scores
            if (input.QuizDetails.All(q => q.Score >= 80))
            {
                itemProgress.Completed = true;
            }

            progress.LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Get the appropriate progress dictionary based on activity type
        /// </summary>
        public Dictionary<string, ECDGamesActivityProgress> GetProgressDictionary(ChildProgress progress, ECDGameActivityName activityType)
        {
            switch (activityType)
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

        /// <summary>
        /// Get the appropriate activity stats based on quiz type
        /// </summary>
        public ActivityStats GetActivityStats(ECDGamesActivityProgress progress, QuizTypeDetail quizType)
        {
            switch (quizType)
            {
                case QuizTypeDetail.Listening:
                    return progress.HearingQuiz;
                case QuizTypeDetail.TextToFigure:
                    return progress.TextToFigureQuiz;
                case QuizTypeDetail.FiguresToText:
                    return progress.FigureToTextQuiz;
                case QuizTypeDetail.Counting:
                    return progress.CountingQuiz;
                case QuizTypeDetail.BubblePopActivity:
                    return progress.BubblePop;
                case QuizTypeDetail.ObjectRecognition:
                    return progress.ObjectRecognitionQuiz;
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Analysis logic for identifying weaknesses and strengths
    /// </summary>
    public class AnalysisLogic
    {
        /// <summary>
        /// Analyze child's progress and identify weaknesses
        /// </summary>
        public WeaknessAnalysis AnalyzeWeaknesses(ChildProgress progress, int topWeakItems = 5)
        {
            var analysis = new WeaknessAnalysis();

            // Analyze each category
            analysis.WeakNumbers = GetWeakItems(progress.NumberProgressDict, "Numbers", topWeakItems);
            analysis.WeakCapitalAlphabets = GetWeakItems(progress.CapitalAlphabetProgressDict, "Capital Letters", topWeakItems);
            analysis.WeakSmallAlphabets = GetWeakItems(progress.SmallAlphabetProgressDict, "Small Letters", topWeakItems);
            analysis.WeakShapes = GetWeakItems(progress.ShapeProgressDict, "Shapes", topWeakItems);
            analysis.WeakColors = GetWeakItems(progress.ColorProgressDict, "Colors", topWeakItems);

            // Get weakest activities across all items
            analysis.WeakestActivities = GetWeakestActivities(progress);

            return analysis;
        }

        /// <summary>
        /// Get prioritized focus items from weakness analysis
        /// </summary>
        public List<FocusItem> GetPrioritizedFocusItems(WeaknessAnalysis analysis, int maxItems = 10)
        {
            var allWeakItems = new List<FocusItem>();

            // Add all weak items with their category context
            allWeakItems.AddRange(analysis.WeakNumbers.Select(w => new FocusItem
            {
                Category = ECDGameActivityName.Numbers,
                ItemName = w.ItemName,
                Score = w.OverallScore,
                WeakestActivity = w.WeakestActivity,
                Priority = CalculatePriority(w)
            }));

            allWeakItems.AddRange(analysis.WeakCapitalAlphabets.Select(w => new FocusItem
            {
                Category = ECDGameActivityName.CapitalAlphabet,
                ItemName = w.ItemName,
                Score = w.OverallScore,
                WeakestActivity = w.WeakestActivity,
                Priority = CalculatePriority(w)
            }));

            allWeakItems.AddRange(analysis.WeakSmallAlphabets.Select(w => new FocusItem
            {
                Category = ECDGameActivityName.SmallAlphabet,
                ItemName = w.ItemName,
                Score = w.OverallScore,
                WeakestActivity = w.WeakestActivity,
                Priority = CalculatePriority(w)
            }));

            allWeakItems.AddRange(analysis.WeakShapes.Select(w => new FocusItem
            {
                Category = ECDGameActivityName.Shapes,
                ItemName = w.ItemName,
                Score = w.OverallScore,
                WeakestActivity = w.WeakestActivity,
                Priority = CalculatePriority(w)
            }));

            allWeakItems.AddRange(analysis.WeakColors.Select(w => new FocusItem
            {
                Category = ECDGameActivityName.Colors,
                ItemName = w.ItemName,
                Score = w.OverallScore,
                WeakestActivity = w.WeakestActivity,
                Priority = CalculatePriority(w)
            }));

            return allWeakItems
                .OrderByDescending(item => item.Priority)
                .Take(maxItems)
                .ToList();
        }

        /// <summary>
        /// Calculate overall score for an activity progress item
        /// </summary>
        public double CalculateOverallScore(ECDGamesActivityProgress progress)
        {
            double totalScore = 0;
            int components = 0;

            // Tracing score
            if (progress.TotalStars > 0)
            {
                totalScore += (double)progress.TotalStarsAchieved / progress.TotalStars * 100;
                components++;
            }

            // Quiz score
            if (progress.QuizCount > 0)
            {
                totalScore += (double)(progress.QuizCount - progress.QuizFailCount) / progress.QuizCount * 100;
                components++;
            }

            // Individual activity scores
            var activities = new[]
            {
                progress.ObjectRecognitionQuiz, progress.HearingQuiz, progress.TextToFigureQuiz,
                progress.FigureToTextQuiz, progress.CountingQuiz, progress.BubblePop
            };

            foreach (var activity in activities)
            {
                if (activity != null && activity.Count > 0)
                {
                    totalScore += activity.SuccessRate;
                    components++;
                }
            }

            return components > 0 ? totalScore / components : 0;
        }

        /// <summary>
        /// Get the weakest activity for an item
        /// </summary>
        public string GetWeakestActivity(ECDGamesActivityProgress progress)
        {
            var activities = new Dictionary<string, double>
            {
                ["Tracing"] = progress.TracingCount > 0 ? (double)progress.TracingCompleteCount / progress.TracingCount * 100 : 100,
                ["Object Recognition"] = progress.ObjectRecognitionQuiz?.SuccessRate ?? 100,
                ["Hearing"] = progress.HearingQuiz?.SuccessRate ?? 100,
                ["Text to Figure"] = progress.TextToFigureQuiz?.SuccessRate ?? 100,
                ["Figure to Text"] = progress.FigureToTextQuiz?.SuccessRate ?? 100,
                ["Counting"] = progress.CountingQuiz?.SuccessRate ?? 100,
                ["Bubble Pop"] = progress.BubblePop?.SuccessRate ?? 100
            };

            return activities.OrderBy(a => a.Value).First().Key;
        }

        /// <summary>
        /// Get total attempts for an activity progress item
        /// </summary>
        public int GetTotalAttempts(ECDGamesActivityProgress progress)
        {
            return progress.TracingCount + progress.QuizCount;
        }

        /// <summary>
        /// Get total time spent on an activity progress item
        /// </summary>
        public double GetTotalTimeSpent(ECDGamesActivityProgress progress)
        {
            return progress.TracingTotalTime + progress.QuizTotalTime;
        }

        /// <summary>
        /// Get completion rate for an activity progress item
        /// </summary>
        public double GetCompletionRate(ECDGamesActivityProgress progress)
        {
            if (progress.TracingCount == 0 && progress.QuizCount == 0) return 0;

            double tracingRate = progress.TracingCount > 0 ? (double)progress.TracingCompleteCount / progress.TracingCount : 0;
            double quizRate = progress.QuizCount > 0 ? (double)(progress.QuizCount - progress.QuizFailCount) / progress.QuizCount : 0;

            return ((tracingRate + quizRate) / 2) * 100;
        }

        private List<WeakItem> GetWeakItems(Dictionary<string, ECDGamesActivityProgress> progressDict, string category, int topCount)
        {
            if (progressDict == null || progressDict.Count == 0)
                return new List<WeakItem>();

            return progressDict
                .Select(kvp => new WeakItem
                {
                    Category = category,
                    ItemName = kvp.Value.ItemName,
                    OverallScore = CalculateOverallScore(kvp.Value),
                    WeakestActivity = GetWeakestActivity(kvp.Value),
                    Completed = kvp.Value.Completed,
                    TotalAttempts = GetTotalAttempts(kvp.Value),
                    TotalTimeSpent = GetTotalTimeSpent(kvp.Value),
                    CompletionRate = GetCompletionRate(kvp.Value)
                })
                .Where(item => !item.Completed || item.OverallScore < 70)
                .OrderBy(item => item.OverallScore)
                .ThenBy(item => item.CompletionRate)
                .Take(topCount)
                .ToList();
        }

        private List<ActivityWeakness> GetWeakestActivities(ChildProgress progress)
        {
            var activityTypes = new[]
            {
                "Tracing", "ObjectRecognition", "Hearing", "TextToFigure",
                "FigureToText", "Counting", "BubblePop"
            };

            var weaknesses = new List<ActivityWeakness>();

            foreach (var activityType in activityTypes)
            {
                var stats = GatherActivityStats(progress, activityType);
                if (stats.TotalAttempts > 0)
                {
                    weaknesses.Add(new ActivityWeakness
                    {
                        ActivityType = activityType,
                        AverageSuccessRate = stats.SuccessRate,
                        TotalAttempts = stats.TotalAttempts,
                        TotalFailures = stats.TotalFailures,
                        AverageTime = stats.AverageTime
                    });
                }
            }

            return weaknesses
                .OrderBy(w => w.AverageSuccessRate)
                .ThenByDescending(w => w.TotalFailures)
                .Take(5)
                .ToList();
        }

        private (double SuccessRate, int TotalAttempts, int TotalFailures, double AverageTime)
            GatherActivityStats(ChildProgress progress, string activityType)
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
            int totalFailures = 0;
            double totalTime = 0;

            foreach (var dict in allDicts)
            {
                if (dict == null) continue;

                foreach (var itemProgress in dict.Values)
                {
                    switch (activityType)
                    {
                        case "Tracing":
                            totalAttempts += itemProgress.TracingCount;
                            totalSuccess += itemProgress.TracingCompleteCount;
                            totalFailures += itemProgress.TracingCount - itemProgress.TracingCompleteCount;
                            totalTime += itemProgress.TracingTotalTime;
                            break;

                        case "ObjectRecognition":
                            AddActivityStatsData(itemProgress.ObjectRecognitionQuiz);
                            break;

                        case "Hearing":
                            AddActivityStatsData(itemProgress.HearingQuiz);
                            break;

                        case "TextToFigure":
                            AddActivityStatsData(itemProgress.TextToFigureQuiz);
                            break;

                        case "FigureToText":
                            AddActivityStatsData(itemProgress.FigureToTextQuiz);
                            break;

                        case "Counting":
                            AddActivityStatsData(itemProgress.CountingQuiz);
                            break;

                        case "BubblePop":
                            AddActivityStatsData(itemProgress.BubblePop);
                            break;
                    }
                }
            }

            void AddActivityStatsData(ActivityStats stats)
            {
                if (stats != null)
                {
                    totalAttempts += stats.Count;
                    totalSuccess += stats.Count - stats.FailCount;
                    totalFailures += stats.FailCount;
                    totalTime += stats.TotalTime;
                }
            }

            double successRate = totalAttempts > 0 ? (double)totalSuccess / totalAttempts * 100 : 0;
            double avgTime = totalAttempts > 0 ? totalTime / totalAttempts : 0;

            return (successRate, totalAttempts, totalFailures, avgTime);
        }

        private double CalculatePriority(WeakItem item)
        {
            double priorityScore = 100 - item.OverallScore;

            if (item.TotalAttempts < 3)
                priorityScore += 20;

            if (!item.Completed)
                priorityScore += 10;

            return priorityScore;
        }

        /// <summary>
        /// Get expected item count for a category
        /// </summary>
        public int GetExpectedItemCount(ECDGameActivityName category)
        {
            switch (category)
            {
                case ECDGameActivityName.Numbers:
                    return 10; // 1-10
                case ECDGameActivityName.CapitalAlphabet:
                    return 26; // A-Z
                case ECDGameActivityName.SmallAlphabet:
                    return 26; // a-z
                case ECDGameActivityName.Shapes:
                    return 10; // Assuming 10 basic shapes
                case ECDGameActivityName.Colors:
                    return 12; // Assuming 12 basic colors
                default:
                    return 10;
            }
        }
    }

    /// <summary>
    /// Recommendation logic for generating activity suggestions
    /// </summary>
    public class RecommendationLogic
    {
        private readonly AnalysisLogic _analysisLogic = new AnalysisLogic();

        /// <summary>
        /// Generate activity recommendations based on weakness analysis
        /// </summary>
        public List<ActivityRecommendation> GenerateRecommendations(ChildProgress progress, WeaknessAnalysis analysis)
        {
            var recommendations = new List<ActivityRecommendation>();

            // Recommend based on weakest activities
            foreach (var weakness in analysis.WeakestActivities.Take(3))
            {
                var items = GetItemsForWeakActivity(progress, weakness.ActivityType);

                recommendations.Add(new ActivityRecommendation
                {
                    ActivityType = weakness.ActivityType,
                    Reason = $"Success rate is only {weakness.AverageSuccessRate:F0}%",
                    SuggestedItems = items,
                    Priority = weakness.AverageSuccessRate < 50 ? 1 : 2
                });
            }

            // Add recommendations for categories with low completion
            var categoryProgress = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            foreach (var (category, dict) in categoryProgress)
            {
                if (dict == null) continue;

                var completionRate = (double)dict.Count(p => p.Value.Completed) / _analysisLogic.GetExpectedItemCount(category) * 100;

                if (completionRate < 30)
                {
                    recommendations.Add(new ActivityRecommendation
                    {
                        ActivityType = "Completion Focus",
                        Reason = $"Only {completionRate:F0}% of {category} completed",
                        SuggestedItems = GetUncompletedItems(dict),
                        Priority = 1
                    });
                }
            }

            return recommendations.OrderBy(r => r.Priority).ToList();
        }

        /// <summary>
        /// Generate focused learning plan
        /// </summary>
        public FocusedLearningPlan GenerateLearningPlan(WeaknessAnalysis analysis, int planDuration = 7)
        {
            var plan = new FocusedLearningPlan
            {
                GeneratedDate = DateTime.UtcNow,
                PlanDuration = planDuration,
                DailyFocusItems = new List<DailyFocus>()
            };

            var priorityItems = _analysisLogic.GetPrioritizedFocusItems(analysis, planDuration * 3);

            for (int day = 0; day < planDuration; day++)
            {
                var dailyFocus = new DailyFocus
                {
                    Day = day + 1,
                    Date = DateTime.UtcNow.Date.AddDays(day),
                    FocusItems = new List<FocusActivity>(),
                    EstimatedDuration = 0
                };

                var itemsForDay = priorityItems.Skip(day * 3).Take(3).ToList();

                foreach (var item in itemsForDay)
                {
                    var focusActivity = new FocusActivity
                    {
                        Category = item.Category,
                        ItemName = item.ItemName,
                        ActivityType = item.WeakestActivity,
                        TargetScore = 80,
                        EstimatedMinutes = 5,
                        Reason = $"Current score: {item.Score:F0}%"
                    };

                    dailyFocus.FocusItems.Add(focusActivity);
                    dailyFocus.EstimatedDuration += focusActivity.EstimatedMinutes;
                }

                plan.DailyFocusItems.Add(dailyFocus);
            }

            return plan;
        }

        private List<string> GetItemsForWeakActivity(ChildProgress progress, string activityType)
        {
            var items = new List<string>();
            var allDicts = new[]
            {
                (ECDGameActivityName.Numbers, progress.NumberProgressDict),
                (ECDGameActivityName.CapitalAlphabet, progress.CapitalAlphabetProgressDict),
                (ECDGameActivityName.SmallAlphabet, progress.SmallAlphabetProgressDict),
                (ECDGameActivityName.Shapes, progress.ShapeProgressDict),
                (ECDGameActivityName.Colors, progress.ColorProgressDict)
            };

            foreach (var (category, dict) in allDicts)
            {
                if (dict == null) continue;

                foreach (var kvp in dict)
                {
                    var itemProgress = kvp.Value;
                    var weakestActivity = _analysisLogic.GetWeakestActivity(itemProgress);

                    if (weakestActivity.Contains(activityType))
                    {
                        items.Add($"{category}: {kvp.Key}");
                    }
                }
            }

            return items.Take(5).ToList();
        }

        private List<string> GetUncompletedItems(Dictionary<string, ECDGamesActivityProgress> dict)
        {
            return dict
                .Where(kvp => !kvp.Value.Completed)
                .Select(kvp => kvp.Key)
                .Take(5)
                .ToList();
        }
    }

    /// <summary>
    /// Validation logic for input data
    /// </summary>
    public class ValidationLogic
    {
        /// <summary>
        /// Validate tracing activity input
        /// </summary>
        public ValidationResult ValidateTracingInput(TracingActivityInput input)
        {
            var result = new ValidationResult();

            if (input == null)
            {
                result.IsValid = false;
                result.Errors.Add("Tracing input cannot be null");
                return result;
            }

            result.Errors.AddRange(input.GetValidationErrors());
            result.IsValid = result.Errors.Count == 0;

            return result;
        }

        /// <summary>
        /// Validate quiz activity input
        /// </summary>
        public ValidationResult ValidateQuizInput(QuizActivityInput input)
        {
            var result = new ValidationResult();

            if (input == null)
            {
                result.IsValid = false;
                result.Errors.Add("Quiz input cannot be null");
                return result;
            }

            result.Errors.AddRange(input.GetValidationErrors());
            result.IsValid = result.Errors.Count == 0;

            return result;
        }

        /// <summary>
        /// Validate batch activity input
        /// </summary>
        public ValidationResult ValidateBatchInput(BatchActivityInput input)
        {
            var result = new ValidationResult();

            if (input == null)
            {
                result.IsValid = false;
                result.Errors.Add("Batch input cannot be null");
                return result;
            }

            result.Errors.AddRange(input.GetValidationErrors());
            result.IsValid = result.Errors.Count == 0;

            return result;
        }
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Errors { get; set; } = new List<string>();
    }
}