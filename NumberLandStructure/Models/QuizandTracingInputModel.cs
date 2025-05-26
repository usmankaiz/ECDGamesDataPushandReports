using System.Collections.Generic;
using NumberLandStructure.Data;

namespace NumberLandStructure.Input
{
    /// <summary>
    /// Input model for tracing activities
    /// </summary>
    public class TracingActivityInput
    {
        public ECDGameActivityName ActivityType { get; set; }
        public string ItemDetails { get; set; }
        public bool Completed { get; set; }
        public int StarsAchieved { get; set; }
        public int MaxStars { get; set; } = 3;
        public double TotalTime { get; set; }

        /// <summary>
        /// Validate the tracing input
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(ItemDetails) &&
                   StarsAchieved >= 0 &&
                   MaxStars > 0 &&
                   StarsAchieved <= MaxStars &&
                   TotalTime >= 0;
        }

        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(ItemDetails))
                errors.Add("Item details cannot be empty");

            if (StarsAchieved < 0)
                errors.Add("Stars achieved cannot be negative");

            if (MaxStars <= 0)
                errors.Add("Max stars must be greater than 0");

            if (StarsAchieved > MaxStars)
                errors.Add("Stars achieved cannot exceed max stars");

            if (TotalTime < 0)
                errors.Add("Total time cannot be negative");

            return errors;
        }
    }

    /// <summary>
    /// Input model for quiz activities
    /// </summary>
    public class QuizActivityInput
    {
        public ECDGameActivityName ActivityType { get; set; }
        public string ItemDetails { get; set; }
        public List<QuizDetail> QuizDetails { get; set; } = new List<QuizDetail>();

        /// <summary>
        /// Validate the quiz input
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(ItemDetails))
                return false;

            if (QuizDetails == null || QuizDetails.Count == 0)
                return false;

            foreach (var quiz in QuizDetails)
            {
                if (!quiz.IsValid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(ItemDetails))
                errors.Add("Item details cannot be empty");

            if (QuizDetails == null || QuizDetails.Count == 0)
                errors.Add("Quiz details cannot be empty");

            if (QuizDetails != null)
            {
                for (int i = 0; i < QuizDetails.Count; i++)
                {
                    var quizErrors = QuizDetails[i].GetValidationErrors();
                    foreach (var error in quizErrors)
                    {
                        errors.Add($"Quiz {i + 1}: {error}");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Get overall quiz performance summary
        /// </summary>
        public QuizPerformanceSummary GetPerformanceSummary()
        {
            if (QuizDetails == null || QuizDetails.Count == 0)
                return new QuizPerformanceSummary();

            int totalQuizzes = QuizDetails.Count;
            int passedQuizzes = 0;
            int failedQuizzes = 0;
            int timeoutQuizzes = 0;
            double totalTime = 0;
            double totalScore = 0;

            foreach (var quiz in QuizDetails)
            {
                totalTime += quiz.TimeTaken;
                totalScore += quiz.Score;

                if (quiz.QuizTimeOut)
                    timeoutQuizzes++;
                else if (quiz.LivesRemaining == 0 || quiz.Score < 50)
                    failedQuizzes++;
                else
                    passedQuizzes++;
            }

            return new QuizPerformanceSummary
            {
                TotalQuizzes = totalQuizzes,
                PassedQuizzes = passedQuizzes,
                FailedQuizzes = failedQuizzes,
                TimeoutQuizzes = timeoutQuizzes,
                AverageScore = totalScore / totalQuizzes,
                TotalTime = totalTime,
                AverageTime = totalTime / totalQuizzes
            };
        }
    }

    /// <summary>
    /// Quiz detail information
    /// </summary>
    public class QuizDetail
    {
        public QuizTypeDetail Type { get; set; }
        public bool Completed { get; set; }
        public int TotalLives { get; set; }
        public int LivesRemaining { get; set; }
        public bool QuizTimeOut { get; set; }
        public double TimeTaken { get; set; }
        public double TimeGiven { get; set; }
        public int Score { get; set; } // 0-100

        /// <summary>
        /// Validate the quiz detail
        /// </summary>
        public bool IsValid()
        {
            return TotalLives > 0 &&
                   LivesRemaining >= 0 &&
                   LivesRemaining <= TotalLives &&
                   TimeTaken >= 0 &&
                   TimeGiven > 0 &&
                   Score >= 0 &&
                   Score <= 100;
        }

        /// <summary>
        /// Get validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (TotalLives <= 0)
                errors.Add("Total lives must be greater than 0");

            if (LivesRemaining < 0)
                errors.Add("Lives remaining cannot be negative");

            if (LivesRemaining > TotalLives)
                errors.Add("Lives remaining cannot exceed total lives");

            if (TimeTaken < 0)
                errors.Add("Time taken cannot be negative");

            if (TimeGiven <= 0)
                errors.Add("Time given must be greater than 0");

            if (Score < 0 || Score > 100)
                errors.Add("Score must be between 0 and 100");

            return errors;
        }

        /// <summary>
        /// Check if the quiz was failed
        /// </summary>
        public bool IsFailed()
        {
            return LivesRemaining == 0 || Score < 50 || QuizTimeOut;
        }

        /// <summary>
        /// Get quiz performance level
        /// </summary>
        public QuizPerformanceLevel GetPerformanceLevel()
        {
            if (IsFailed())
                return QuizPerformanceLevel.Failed;

            if (Score >= 90)
                return QuizPerformanceLevel.Excellent;

            if (Score >= 80)
                return QuizPerformanceLevel.Good;

            if (Score >= 70)
                return QuizPerformanceLevel.Average;

            return QuizPerformanceLevel.BelowAverage;
        }
    }

    /// <summary>
    /// Batch input for multiple activities
    /// </summary>
    public class BatchActivityInput
    {
        public string UserId { get; set; }
        public List<TracingActivityInput> TracingActivities { get; set; } = new List<TracingActivityInput>();
        public List<QuizActivityInput> QuizActivities { get; set; } = new List<QuizActivityInput>();

        /// <summary>
        /// Validate all activities in the batch
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(UserId))
                return false;

            foreach (var tracing in TracingActivities)
            {
                if (!tracing.IsValid())
                    return false;
            }

            foreach (var quiz in QuizActivities)
            {
                if (!quiz.IsValid())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Get all validation errors
        /// </summary>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(UserId))
                errors.Add("User ID cannot be empty");

            for (int i = 0; i < TracingActivities.Count; i++)
            {
                var tracingErrors = TracingActivities[i].GetValidationErrors();
                foreach (var error in tracingErrors)
                {
                    errors.Add($"Tracing Activity {i + 1}: {error}");
                }
            }

            for (int i = 0; i < QuizActivities.Count; i++)
            {
                var quizErrors = QuizActivities[i].GetValidationErrors();
                foreach (var error in quizErrors)
                {
                    errors.Add($"Quiz Activity {i + 1}: {error}");
                }
            }

            return errors;
        }
    }

    /// <summary>
    /// Supporting enums and data structures
    /// </summary>
    public enum QuizPerformanceLevel
    {
        Failed,
        BelowAverage,
        Average,
        Good,
        Excellent
    }

    public class QuizPerformanceSummary
    {
        public int TotalQuizzes { get; set; }
        public int PassedQuizzes { get; set; }
        public int FailedQuizzes { get; set; }
        public int TimeoutQuizzes { get; set; }
        public double AverageScore { get; set; }
        public double TotalTime { get; set; }
        public double AverageTime { get; set; }
        public double PassRate => TotalQuizzes > 0 ? (double)PassedQuizzes / TotalQuizzes * 100 : 0;
    }

    /// <summary>
    /// Input builder helpers
    /// </summary>
    public static class InputBuilder
    {
        public static TracingActivityInput CreateTracingInput(
            ECDGameActivityName activityType,
            string itemDetails,
            bool completed,
            int starsAchieved,
            double totalTime,
            int maxStars = 3)
        {
            return new TracingActivityInput
            {
                ActivityType = activityType,
                ItemDetails = itemDetails,
                Completed = completed,
                StarsAchieved = starsAchieved,
                MaxStars = maxStars,
                TotalTime = totalTime
            };
        }

        public static QuizDetail CreateQuizDetail(
            QuizTypeDetail type,
            bool completed,
            int totalLives,
            int livesRemaining,
            bool timeout,
            double timeTaken,
            double timeGiven,
            int score)
        {
            return new QuizDetail
            {
                Type = type,
                Completed = completed,
                TotalLives = totalLives,
                LivesRemaining = livesRemaining,
                QuizTimeOut = timeout,
                TimeTaken = timeTaken,
                TimeGiven = timeGiven,
                Score = score
            };
        }

        public static QuizActivityInput CreateQuizInput(
            ECDGameActivityName activityType,
            string itemDetails,
            params QuizDetail[] quizDetails)
        {
            return new QuizActivityInput
            {
                ActivityType = activityType,
                ItemDetails = itemDetails,
                QuizDetails = new List<QuizDetail>(quizDetails)
            };
        }
    }
}