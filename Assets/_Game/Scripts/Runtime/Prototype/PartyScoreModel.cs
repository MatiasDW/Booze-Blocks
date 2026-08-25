using System;

namespace BoozeBlocks.Prototype
{
    public sealed class PartyScoreModel
    {
        public int Score { get; private set; }
        public int Servings { get; private set; }
        public int Barricades { get; private set; }
        public int Distractions { get; private set; }
        public int BurnedMeals { get; private set; }
        public int Rescues { get; private set; }

        public void AddServing()
        {
            Servings++;
            Score += 250;
        }

        public void AddBarricade()
        {
            Barricades++;
            Score += 35;
        }

        public void AddDistraction()
        {
            Distractions++;
            Score += 60;
        }

        public void AddWaveBonus(int wave)
        {
            Score += 100 + Math.Max(1, wave) * 25;
        }

        public void AddBurnedMeal()
        {
            BurnedMeals++;
            Score = Math.Max(0, Score - 40);
        }

        public void AddRescue()
        {
            Rescues++;
            Score += 120;
        }

        public void ApplySnapshot(int score, int servings, int barricades, int distractions,
            int burnedMeals, int rescues)
        {
            Score = Math.Max(0, score);
            Servings = Math.Max(0, servings);
            Barricades = Math.Max(0, barricades);
            Distractions = Math.Max(0, distractions);
            BurnedMeals = Math.Max(0, burnedMeals);
            Rescues = Math.Max(0, rescues);
        }
    }
}
