# VR_TSA

Unity project: `New Unity Project`

- Unity Editor: `6000.5.10f1`
- Unity Cloud organization: `shivvesh2009`
- Unity Cloud project ID: `5eff2aea-bc97-4492-9b91-da4a12055be2`
- GitHub repository: `pradyunvishu-design/VR_TSA`

## Codex and Unity CLI

The project is linked to Unity Cloud and the Unity Pipeline package is installed. Unity CLI's Codex skill and MCP server are configured on the development Mac, pinned to this project. Restart Codex after changing MCP configuration, then open the project in Unity Editor before using editor-control commands.

Useful checks:

```bash
unity auth status
unity projects info "New Unity Project" --json
unity status
```

## Windows Codex workspace

This checkout is connected to `origin/main` at `C:\VR_TSA`. Edit the repository project at `C:\VR_TSA\New Unity Project`. The separate local `My project` starter folder is not the repository project.

`AGENTS.md` instructs Codex to verify, commit, and push its completed changes after each change task. This is an agent workflow, not a background file watcher; failed pushes must be resolved and retried.

Unity CLI reads this same local project, so no second Git push to the CLI is needed. The repository requires Unity Editor `6000.5.10f1`; do not open it with a newer Editor unless intentionally upgrading.

## GitHub push checks

`.github/workflows/unity-cloud.yml` runs for every push. It installs Unity CLI, verifies the Unity project files, and checks that the committed organization and cloud project IDs have not drifted.

For a live account-access check on each push, create a Unity service account with access to the `New Unity Project` cloud project, then add these optional repository secrets under **GitHub → Settings → Secrets and variables → Actions**:

- `UNITY_SERVICE_ACCOUNT_ID`
- `UNITY_SERVICE_ACCOUNT_SECRET`

The workflow deliberately verifies the connection instead of downloading a multi-gigabyte Editor and producing a build on every commit. Unity Build Automation can be enabled separately if every push should also produce a headset build.
