# Project workflow

- Repository: https://github.com/pradyunvishu-design/VR_TSA.git; primary branch: main.
- Work on the existing Unity project in `New Unity Project`. `My project` is a separate local starter project; do not merge or publish it unless requested.
- Preserve Unity .meta files and GUIDs. Do not commit generated Library, Temp, Logs, builds, or credentials.
- The user authorizes committing and pushing completed Codex changes to origin after each change task. Review the diff, run relevant available checks, stage only task-related files, commit, and push the current branch. Do not force-push or include unrelated user edits. Report any authentication, permission, conflict, or test blocker and whether the push succeeded.
- GitHub Actions runs `.github/workflows/unity-cloud.yml` on pushes. It verifies the Unity Cloud link; it does not upload game assets to Unity Cloud or build a player.
- Unity CLI operates on the local project. Use explicit project paths and the Editor version in ProjectSettings/ProjectVersion.txt. Do not silently upgrade the project.
