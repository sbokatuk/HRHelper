#!/usr/bin/env bash
set -euo pipefail

# Usage:
# PROJECT_ID=hrhelper-57162 REGION=us-central1 ADMIN_PASSWORD=xxxx SA_KEY=/path/key.json ./scripts/deploy-cloudrun-source.sh

: "${PROJECT_ID:?PROJECT_ID is required}"
: "${REGION:=us-central1}"
: "${ADMIN_PASSWORD:?ADMIN_PASSWORD is required}"
: "${SA_KEY:?SA_KEY is required (path to service account JSON)}"

if ! command -v gcloud >/dev/null 2>&1; then
  echo "gcloud not found. Install Google Cloud SDK." >&2
  exit 1
fi
if ! command -v firebase >/dev/null 2>&1; then
  echo "firebase CLI not found. Install: npm i -g firebase-tools" >&2
  exit 1
fi

# Auth
gcloud auth activate-service-account --key-file "$SA_KEY"
gcloud config set project "$PROJECT_ID"
gcloud services enable run.googleapis.com cloudbuild.googleapis.com firebase.googleapis.com firebasehosting.googleapis.com --quiet

# Deploy from source (uses Cloud Build)

echo "Deploying Cloud Run service hrhelper from source..."
gcloud run deploy hrhelper \
  --source . \
  --region "$REGION" \
  --platform managed \
  --allow-unauthenticated \
  --set-env-vars ADMIN_PASSWORD="$ADMIN_PASSWORD"

# Update Firebase project mapping
if command -v jq >/dev/null 2>&1; then
  tmp=$(mktemp)
  jq ".projects.default = \"$PROJECT_ID\"" .firebaserc > "$tmp" && mv "$tmp" .firebaserc
fi

export GOOGLE_APPLICATION_CREDENTIALS="$SA_KEY"

echo "Deploying Firebase Hosting..."
firebase --project "$PROJECT_ID" deploy --only hosting --non-interactive

echo "Done."
