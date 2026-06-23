🔒 Fix uncaught exceptions in file handling

🎯 What: Replaced empty catch blocks with exception logging in Game1.cs settings file operations.
⚠️ Risk: If file reading/writing fails, the game silently fails which hides issues and makes troubleshooting impossible, potentially losing user data or putting the game into an unknown state.
🛡️ Solution: Catches Exception and uses PitLife.Core.Logger to write the failure reason, restoring visibility to file IO issues.
