using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace NumberLandStructure.Data
{
    public enum ProgressEventType
    {
        Daily,
        Weekly,
        Monthly,
        Yearly
    }

    public enum ECDGameActivityName
    {
        Numbers,
        CapitalAlphabet,
        SmallAlphabet,
        Shapes,
        Colors
    }

    public enum QuizTypeDetail
    {
        ObjectRecognition,
        Counting,
        FiguresToText,
        TextToFigure,
        Listening,
        BubblePopActivity
    }

    /// <summary>
    /// Main progress tracking class optimized for MongoDB
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ChildProgress
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("userId")]
        public string UserId { get; set; }

        [BsonElement("createdAt")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; }

        [BsonElement("periodStart")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime PeriodStart { get; set; }

        [BsonElement("eventType")]
        [BsonRepresentation(BsonType.String)]
        public ProgressEventType EventType { get; set; }

        [BsonElement("numberProgress")]
        [BsonIgnoreIfNull]
        public Dictionary<string, ECDGamesActivityProgress> NumberProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

        [BsonElement("capitalAlphabetProgress")]
        [BsonIgnoreIfNull]
        public Dictionary<string, ECDGamesActivityProgress> CapitalAlphabetProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

        [BsonElement("smallAlphabetProgress")]
        [BsonIgnoreIfNull]
        public Dictionary<string, ECDGamesActivityProgress> SmallAlphabetProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

        [BsonElement("shapeProgress")]
        [BsonIgnoreIfNull]
        public Dictionary<string, ECDGamesActivityProgress> ShapeProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

        [BsonElement("colorProgress")]
        [BsonIgnoreIfNull]
        public Dictionary<string, ECDGamesActivityProgress> ColorProgressDict { get; set; } = new Dictionary<string, ECDGamesActivityProgress>();

        [BsonElement("lastUpdated")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Activity progress for each item (number, letter, shape, etc.)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ECDGamesActivityProgress
    {
        [BsonElement("itemName")]
        public string ItemName { get; set; }

        [BsonElement("completed")]
        public bool Completed { get; set; }

        // Tracing activities
        [BsonElement("tracingCount")]
        public int TracingCount { get; set; }

        [BsonElement("tracingCompleteCount")]
        public int TracingCompleteCount { get; set; }

        [BsonElement("tracingTotalTime")]
        public double TracingTotalTime { get; set; }

        [BsonElement("totalStars")]
        public int TotalStars { get; set; }

        [BsonElement("totalStarsAchieved")]
        public int TotalStarsAchieved { get; set; }

        // Regular quiz activities
        [BsonElement("quizCount")]
        public int QuizCount { get; set; }

        [BsonElement("quizTotalTime")]
        public double QuizTotalTime { get; set; }

        [BsonElement("quizFailCount")]
        public int QuizFailCount { get; set; }

        [BsonElement("quizTimeOutCount")]
        public int QuizTimeOutCount { get; set; }

        // Individual activity types
        [BsonElement("objectRecognitionQuiz")]
        public ActivityStats ObjectRecognitionQuiz { get; set; } = new ActivityStats();

        [BsonElement("hearingQuiz")]
        public ActivityStats HearingQuiz { get; set; } = new ActivityStats();

        [BsonElement("textToFigureQuiz")]
        public ActivityStats TextToFigureQuiz { get; set; } = new ActivityStats();

        [BsonElement("figureToTextQuiz")]
        public ActivityStats FigureToTextQuiz { get; set; } = new ActivityStats();

        [BsonElement("countingQuiz")]
        public ActivityStats CountingQuiz { get; set; } = new ActivityStats();

        [BsonElement("bubblePop")]
        public ActivityStats BubblePop { get; set; } = new ActivityStats();
    }

    /// <summary>
    /// Statistics for individual activity types
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ActivityStats
    {
        [BsonElement("count")]
        public int Count { get; set; }

        [BsonElement("failCount")]
        public int FailCount { get; set; }

        [BsonElement("totalTime")]
        public double TotalTime { get; set; }

        [BsonElement("totalFailTime")]
        public double TotalFailTime { get; set; }

        [BsonElement("quizTimeOutCount")]
        public int QuizTimeOutCount { get; set; }

        [BsonIgnore]
        public double SuccessRate => Count > 0 ? ((double)(Count - FailCount) / Count) * 100 : 0;

        [BsonIgnore]
        public double AverageTime => Count > 0 ? TotalTime / Count : 0;
    }

    /// <summary>
    /// Analysis and report data models
    /// </summary>
    public class WeaknessAnalysis
    {
        public List<WeakItem> WeakNumbers { get; set; } = new List<WeakItem>();
        public List<WeakItem> WeakCapitalAlphabets { get; set; } = new List<WeakItem>();
        public List<WeakItem> WeakSmallAlphabets { get; set; } = new List<WeakItem>();
        public List<WeakItem> WeakShapes { get; set; } = new List<WeakItem>();
        public List<WeakItem> WeakColors { get; set; } = new List<WeakItem>();
        public List<ActivityWeakness> WeakestActivities { get; set; } = new List<ActivityWeakness>();
    }

    public class WeakItem
    {
        public string Category { get; set; }
        public string ItemName { get; set; }
        public double OverallScore { get; set; }
        public string WeakestActivity { get; set; }
        public bool Completed { get; set; }
        public int TotalAttempts { get; set; }
        public double TotalTimeSpent { get; set; }
        public double CompletionRate { get; set; }
    }

    public class ActivityWeakness
    {
        public string ActivityType { get; set; }
        public double AverageSuccessRate { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalFailures { get; set; }
        public double AverageTime { get; set; }
        public Dictionary<ECDGameActivityName, List<string>> WeakItemsByCategory { get; set; } = new Dictionary<ECDGameActivityName, List<string>>();
    }

    public class FocusItem
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public double Score { get; set; }
        public string WeakestActivity { get; set; }
        public double Priority { get; set; }
    }

    /// <summary>
    /// Report data models
    /// </summary>
    public class ParentProgressReport
    {
        public string ChildUserId { get; set; }
        public DateTime ReportDate { get; set; }
        public ProgressEventType ReportPeriod { get; set; }
        public int TotalActivitiesCompleted { get; set; }
        public double TotalTimeSpent { get; set; }
        public double OverallSuccessRate { get; set; }
        public Dictionary<ECDGameActivityName, CategoryProgressSummary> CategoryProgress { get; set; }
        public Dictionary<string, ActivityPerformanceSummary> ActivityPerformance { get; set; }
        public List<FocusItem> TopPriorityItems { get; set; }
        public List<ActivityRecommendation> ActivityRecommendations { get; set; }
    }

    public class CategoryProgressSummary
    {
        public ECDGameActivityName Category { get; set; }
        public int TotalItems { get; set; }
        public int ItemsCompleted { get; set; }
        public int ItemsAttempted { get; set; }
        public double CompletionRate { get; set; }
        public double SuccessRate { get; set; }
        public double TimeSpent { get; set; }
        public List<string> WeakItems { get; set; } = new List<string>();
        public List<string> StrongItems { get; set; } = new List<string>();
    }

    public class ActivityPerformanceSummary
    {
        public string ActivityType { get; set; }
        public int TotalAttempts { get; set; }
        public double SuccessRate { get; set; }
        public double AverageTime { get; set; }
        public bool NeedsImprovement { get; set; }
        public List<string> WeakCategories { get; set; } = new List<string>();
    }

    public class ActivityRecommendation
    {
        public string ActivityType { get; set; }
        public string Reason { get; set; }
        public List<string> SuggestedItems { get; set; } = new List<string>();
        public int Priority { get; set; }
    }

    /// <summary>
    /// Learning plan data models
    /// </summary>
    public class FocusedLearningPlan
    {
        public DateTime GeneratedDate { get; set; }
        public int PlanDuration { get; set; }
        public List<DailyFocus> DailyFocusItems { get; set; }
    }

    public class DailyFocus
    {
        public int Day { get; set; }
        public DateTime Date { get; set; }
        public List<FocusActivity> FocusItems { get; set; }
        public int EstimatedDuration { get; set; }
    }

    public class FocusActivity
    {
        public ECDGameActivityName Category { get; set; }
        public string ItemName { get; set; }
        public string ActivityType { get; set; }
        public int TargetScore { get; set; }
        public int EstimatedMinutes { get; set; }
        public string Reason { get; set; }
    }

    /// <summary>
    /// Activity weakness details
    /// </summary>
    public class ActivityWeaknessDetails
    {
        public string ActivityType { get; set; }
        public Dictionary<ECDGameActivityName, List<ItemPerformance>> WeakItemsByCategory { get; set; }
        public double OverallSuccessRate { get; set; }
        public int TotalAttempts { get; set; }
        public List<string> TopWeakItems { get; set; } = new List<string>();
    }

    public class ItemPerformance
    {
        public string ItemName { get; set; }
        public int TotalAttempts { get; set; }
        public int TotalSuccess { get; set; }
        public double TotalTime { get; set; }
        public double SuccessRate => TotalAttempts > 0 ? (double)TotalSuccess / TotalAttempts * 100 : 0;
        public double AverageTime => TotalAttempts > 0 ? TotalTime / TotalAttempts : 0;
    }
}