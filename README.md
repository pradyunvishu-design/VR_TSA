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

## Check every GitHub push

`.github/workflows/unity-cloud.yml` runs for every push. It installs Unity CLI, verifies the Unity project files, and checks that the committed organization and cloud project IDs have not drifted.

For a live account-access check on each push, create a Unity service account with access to the `New Unity Project` cloud project, then add these optional repository secrets under **GitHub → Settings → Secrets and variables → Actions**:

- `UNITY_SERVICE_ACCOUNT_ID`
- `UNITY_SERVICE_ACCOUNT_SECRET`

The workflow deliberately verifies the connection instead of downloading a multi-gigabyte Editor and producing a build on every commit. Unity Build Automation can be enabled separately if every push should also produce a headset build.
