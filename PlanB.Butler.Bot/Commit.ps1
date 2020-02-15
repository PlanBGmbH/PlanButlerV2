### Script for git commit without .vs folder
Param (
    [Parameter(Mandatory = $true)]
    [string]$commitmessage
)

### Pull Changes to prevent merge conflicts
git pull

### Add Files to commit
git add *

### Commit chagnes
git commit -m $commitmessage

### push changes
git push