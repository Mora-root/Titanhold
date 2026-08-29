using System;

namespace Titanhold.Run
{
    public sealed class RiftInstabilityState
    {
        public RiftInstabilityState(int pointsPerLevel)
        {
            if (pointsPerLevel <= 0)
                throw new ArgumentOutOfRangeException(nameof(pointsPerLevel));

            PointsPerLevel = pointsPerLevel;
        }

        public int Points { get; private set; }
        public int PointsPerLevel { get; }
        public int Level => Points / PointsPerLevel;
        public int PointsIntoCurrentLevel => Points % PointsPerLevel;
        public int PointsToNextLevel => PointsPerLevel - PointsIntoCurrentLevel;

        internal int AddPoints(int amount)
        {
            if (amount <= 0 || Points == int.MaxValue)
                return 0;

            int previousPoints = Points;
            long total = (long)Points + amount;
            Points = total >= int.MaxValue ? int.MaxValue : (int)total;
            return Points - previousPoints;
        }

        internal void Reset()
        {
            Points = 0;
        }
    }
}
