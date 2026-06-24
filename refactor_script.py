import sys
file_path = 'Simulation/Behaviors/FeedingModule.cs'
with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read().replace('\r\n', '\n')

old_herbivore = '''    private static bool TryFeedHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        // Try eating a nearby fruit first
        var fruit = ecosystem.Fruits.TryEatFruit(self.Position, 12f);
        if (fruit.HasValue)
        {
            if (fruit.Value.Poisonous && self.Genome.PlantRecognition < 0.5f)
            {
                self.Energy -= fruit.Value.EnergyValue * 2f;
                self.RememberDanger(fruit.Value.Position);
            }
            else
            {
                self.Energy = Math.Min(self.Energy + fruit.Value.EnergyValue, self.MaxEnergy);
                self.RememberFood(fruit.Value.Position);
            }
            return true;
        }

        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food != null)
        {
            if (self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 10f * dt);
                food.Energy -= eaten;
                if (food.IsPoisonous && self.Genome.PlantRecognition < 0.5f)
                {
                    self.Energy -= eaten * 3f;
                    self.RememberDanger(food.Position);
                }
                else
                {
                    self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
                }
                self.RememberFood(food.Position);
                if (food.Energy <= 0) food.Die(DeathCause.Predation);
            }
            else
            {
                self.MoveToward(food.Position, dt, world);
                TryGraze(self, world, dt);
            }
            return true;
        }

        return TryEatCarcass(self, ecosystem, dt);
    }'''

new_herbivore = '''    private static bool TryFeedHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        // Try eating a nearby fruit first
        var fruit = ecosystem.Fruits.TryEatFruit(self.Position, 12f);
        if (fruit.HasValue)
        {
            if (fruit.Value.Poisonous && self.Genome.PlantRecognition < 0.5f)
            {
                self.Energy -= fruit.Value.EnergyValue * 2f;
                self.RememberDanger(fruit.Value.Position);
            }
            else
            {
                self.Energy = Math.Min(self.Energy + fruit.Value.EnergyValue, self.MaxEnergy);
                self.RememberFood(fruit.Value.Position);
            }
            return true;
        }

        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food == null)
            return TryEatCarcass(self, ecosystem, dt);

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            TryGraze(self, world, dt);
            return true;
        }

        float eaten = Math.Min(food.Energy, 10f * dt);
        food.Energy -= eaten;
        if (food.IsPoisonous && self.Genome.PlantRecognition < 0.5f)
        {
            self.Energy -= eaten * 3f;
            self.RememberDanger(food.Position);
        }
        else
        {
            self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
        }
        self.RememberFood(food.Position);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }'''

old_carnivore = '''    private static bool TryFeedCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is Carnivore carn)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
            {
                if (self.DistanceTo(prey) < 10f)
                {
                    float damage = carn.AttackDamage * dt;
                    prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
                    self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
                    if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
                }
                else
                {
                    self.MoveToward(prey.Position, dt, world);
                }
                return true;
            }
            if (TryEatCarcass(self, ecosystem, dt)) return true;
        }
        return false;
    }'''

new_carnivore = '''    private static bool TryFeedCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return false;

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= self.VisionPixels)
        {
            if (TryEatCarcass(self, ecosystem, dt)) return true;
            return false;
        }

        if (self.DistanceTo(prey) >= 10f)
        {
            self.MoveToward(prey.Position, dt, world);
            return true;
        }

        float damage = carn.AttackDamage * dt;
        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
        self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
        if (prey.Energy <= 0) prey.Die(DeathCause.Predation);

        return true;
    }'''

old_omnivore = '''    private static bool TryFeedOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        bool seekPrey = self.Energy < self.MaxEnergy * 0.4f && ecosystem.Random.NextDouble() < 0.4;
        if (seekPrey)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
            {
                if (self.DistanceTo(prey) < 10f)
                {
                    float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
                    prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
                    self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
                    if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
                }
                else
                {
                    self.MoveToward(prey.Position, dt, world);
                }
                return true;
            }
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food != null)
        {
            if (self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 8f * dt);
                food.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
                if (food.Energy <= 0) food.Die(DeathCause.Predation);
            }
            else
            {
                self.MoveToward(food.Position, dt, world);
            }
            return true;
        }
        return false;
    }'''

new_omnivore = '''    private static bool TryFeedOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        bool seekPrey = self.Energy < self.MaxEnergy * 0.4f && ecosystem.Random.NextDouble() < 0.4;
        if (seekPrey)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
            {
                if (self.DistanceTo(prey) >= 10f)
                {
                    self.MoveToward(prey.Position, dt, world);
                    return true;
                }

                float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
                prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
                self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
                if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
                return true;
            }
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null)
            return false;

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            return true;
        }

        float eaten = Math.Min(food.Energy, 8f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }'''

old_nearby_herbivore = '''    private static bool TryFeedNearbyHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);
        if (food != null && self.DistanceTo(food) < 12f)
        {
            float eaten = Math.Min(food.Energy, 10f * dt);
            food.Energy -= eaten;
            self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
            if (food.Energy <= 0) food.Die(DeathCause.Predation);
            return true;
        }
        return TryGraze(self, world, dt);
    }'''

new_nearby_herbivore = '''    private static bool TryFeedNearbyHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food == null || self.DistanceTo(food) >= 12f)
            return TryGraze(self, world, dt);

        float eaten = Math.Min(food.Energy, 10f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }'''

old_nearby_carnivore = '''    private static bool TryFeedNearbyCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is Carnivore carn)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < 10f)
            {
                float damage = carn.AttackDamage * dt;
                prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
                self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
                if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
                return true;
            }
        }
        return TryEatCarcass(self, ecosystem, dt);
    }'''

new_nearby_carnivore = '''    private static bool TryFeedNearbyCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return TryEatCarcass(self, ecosystem, dt);

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= 10f)
            return TryEatCarcass(self, ecosystem, dt);

        float damage = carn.AttackDamage * dt;
        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
        self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
        if (prey.Energy <= 0) prey.Die(DeathCause.Predation);

        return true;
    }'''

old_nearby_omnivore = '''    private static bool TryFeedNearbyOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey != null && self.DistanceTo(prey) < 10f)
        {
            float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
            prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
            self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
            if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
            return true;
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food != null && self.DistanceTo(food) < 12f)
        {
            float eaten = Math.Min(food.Energy, 8f * dt);
            food.Energy -= eaten;
            self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
            if (food.Energy <= 0) food.Die(DeathCause.Predation);
            return true;
        }
        return TryEatCarcass(self, ecosystem, dt);
    }'''

new_nearby_omnivore = '''    private static bool TryFeedNearbyOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey != null && self.DistanceTo(prey) < 10f)
        {
            float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
            prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
            self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
            if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
            return true;
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null || self.DistanceTo(food) >= 12f)
            return TryEatCarcass(self, ecosystem, dt);

        float eaten = Math.Min(food.Energy, 8f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }'''

if old_herbivore not in content: print("ERROR: old_herbivore not found")
if old_carnivore not in content: print("ERROR: old_carnivore not found")
if old_omnivore not in content: print("ERROR: old_omnivore not found")
if old_nearby_herbivore not in content: print("ERROR: old_nearby_herbivore not found")
if old_nearby_carnivore not in content: print("ERROR: old_nearby_carnivore not found")
if old_nearby_omnivore not in content: print("ERROR: old_nearby_omnivore not found")

content = content.replace(old_herbivore, new_herbivore)
content = content.replace(old_carnivore, new_carnivore)
content = content.replace(old_omnivore, new_omnivore)
content = content.replace(old_nearby_herbivore, new_nearby_herbivore)
content = content.replace(old_nearby_carnivore, new_nearby_carnivore)
content = content.replace(old_nearby_omnivore, new_nearby_omnivore)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print("Done")
