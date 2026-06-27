1. Add a StringBuilder `_tempSb` to `InGameUi.cs` and replace `string tempLabel = $"...";` with appending to `_tempSb` and using `_tempSb.ToString()` or creating a `DrawLine` overload that accepts a `StringBuilder`. Actually, we already did the `StringBuilder` overload.
2. In `InGameUi.cs`'s `DrawInlineBar`, remove `value.ToString()` by using a pre-allocated `StringBuilder` to hold the value. Also make it a non-static instance method so we can use the instance field `StringBuilder`.
3. In `InGameUi.cs`'s `DrawStatistics`, use `StringBuilder` for the species list strings, preventing string allocations for every species and count displayed.
4. Replace `$"{speed:0.#}x"` in `InGameUi.cs`'s `LayoutToolbar`/speed label drawing with a `StringBuilder` to avoid string allocations for the speed label.
5. In `InGameUi.cs`'s `DrawCreature`, use `StringBuilder` instead of string interpolation for the lineage and genetics texts.
6. Verify modifications with `dotnet build` and running `read_file` on `UI/InGameUi.cs`.
7. Complete pre commit steps to ensure proper testing, verification, review, and reflection are done.
