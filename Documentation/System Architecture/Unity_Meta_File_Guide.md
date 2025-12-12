# Unity Meta File Management Guide

## Understanding .meta Files

Unity creates `.meta` files for every asset and folder in your project. These files contain important metadata like GUIDs, import settings, and references.

## Common Issues and Solutions

### Orphaned .meta Files

**Problem**: When folders or files are deleted outside of Unity (via file explorer, terminal, etc.), their corresponding `.meta` files can be left behind, causing Unity to recreate empty folders or show import errors.

**Solution**: Always delete both the asset/folder AND its `.meta` file when removing items outside of Unity.

### Best Practices

#### ✅ **Do This:**
- Delete files/folders through Unity's Project window when possible
- If deleting via file system, always remove the corresponding `.meta` file
- Keep `.meta` files in version control (they contain important references)
- Use Unity's "Reimport" function after external file operations

#### ❌ **Avoid This:**
- Deleting only the `.meta` file (Unity will regenerate it with a new GUID)
- Deleting only the asset (leaves orphaned `.meta` file)
- Ignoring `.meta` files in version control for Assets folder

### Finding Orphaned .meta Files

Use this PowerShell command to find orphaned `.meta` files:

```powershell
Get-ChildItem -Path "Assets" -Recurse -Name "*.meta" | ForEach-Object { 
    $metaPath = "Assets\$_"
    $originalPath = $metaPath -replace '\.meta$', ''
    if (-not (Test-Path $originalPath)) {
        Write-Host "Orphaned meta file: $_"
    }
}
```

### Git and .meta Files

In your `.gitignore`, ensure `.meta` files are tracked:

```gitignore
# DON'T ignore .meta files in Assets folder
# They contain important Unity metadata
!/Assets/**/*.meta
```

## Issue Resolution

### "Meta data file exists but folder can't be found"

This error occurs when:
1. A folder was deleted outside Unity
2. Its `.meta` file remained
3. Unity recreates the empty folder

**Fix:**
1. Delete the empty folder Unity created
2. Delete the orphaned `.meta` file
3. Unity console warning will disappear

### Missing References After File Operations

If you see "Missing (Mono Script)" errors after file operations:
1. Check if `.meta` files were properly moved/deleted
2. Use Unity's "Reimport All" if needed
3. Verify all assets have their corresponding `.meta` files

## Prevention

- **Use Unity's Project window** for file operations when possible
- **Double-check .meta cleanup** when using external tools
- **Commit .meta files** to version control
- **Document file organization changes** for team members