#!/usr/bin/env bash
set -euo pipefail

: "${UNITY_PROJECT_PATH:?UNITY_PROJECT_PATH is required}"
: "${UNITY_CLOUD_ORG:?UNITY_CLOUD_ORG is required}"
: "${UNITY_CLOUD_PROJECT:?UNITY_CLOUD_PROJECT is required}"

unity --version
unity projects verify "$UNITY_PROJECT_PATH" --json

project_info="$(unity projects info "$UNITY_PROJECT_PATH" --json)"
printf '%s\n' "$project_info"

linked_project="$(printf '%s' "$project_info" | jq -r '.data.cloudProjectId // .data.cloudProject.id // empty')"

if [[ "$linked_project" != "$UNITY_CLOUD_PROJECT" ]]; then
  echo "Unity project is linked to $linked_project; expected $UNITY_CLOUD_PROJECT."
  exit 1
fi

if [[ -n "${UNITY_SERVICE_ACCOUNT_ID:-}" && -n "${UNITY_SERVICE_ACCOUNT_SECRET:-}" ]]; then
  unity auth status --json

  cloud_projects="$(unity cloud project list --cloud-org "$UNITY_CLOUD_ORG" --json)"
  printf '%s\n' "$cloud_projects"

  if ! printf '%s' "$cloud_projects" | jq -e --arg id "$UNITY_CLOUD_PROJECT" \
    '.data.projects | any(.id == $id)' >/dev/null; then
    echo "Unity Cloud project $UNITY_CLOUD_PROJECT is not available to this service account."
    exit 1
  fi
else
  echo "Unity service-account secrets are not set; verified the committed cloud link without a live account check."
fi

echo "Verified Unity Cloud project $UNITY_CLOUD_PROJECT for commit ${GITHUB_SHA:-local}."
