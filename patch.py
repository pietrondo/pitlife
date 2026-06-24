with open("Simulation/Behaviors/FeedingModule.cs", "r") as f:
    text = f.read()

import re

# Resolve the big conflict around line 33 to 175
# The master branch introduced exactly the same refactoring but passed `world` to Carnivore/Omnivore in `TryFeedNearby`.
# And duplicated `TryFeedOmnivore` and `Plant? food` block. I'll just restore the file completely from HEAD, as I did it cleanly and correctly.
