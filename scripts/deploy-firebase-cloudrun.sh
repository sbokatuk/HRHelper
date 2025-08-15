#!/usr/bin/env bash
set -euo pipefail

# Usage:
# PROJECT_ID=hrhelper-57162 REGION=us-central1 ADMIN_PASSWORD=xxxx SA_KEY=/path/key.json ./scripts/deploy-firebase-cloudrun.sh

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
if ! command -v docker >/dev/null 2>&1; then
  echo "docker not found. Install Docker Desktop and start the daemon." >&2
  exit 1
fi

# Auth
gcloud auth activate-service-account --key-file "$SA_KEY"
gcloud config set project "$PROJECT_ID"
gcloud services enable run.googleapis.com artifactregistry.googleapis.com firebase.googleapis.com firebasehosting.googleapis.com --quiet

# Create Artifact Registry repo (idempotent)
(gcloud artifacts repositories describe hrhelper --location="$REGION" >/dev/null 2>&1) || \
  gcloud artifacts repositories create hrhelper --repository-format=docker --location="$REGION" --description="HRHelper images"

echo "Configuring Docker auth for Artifact Registry..."
gcloud auth configure-docker "${REGION}-docker.pkg.dev" --quiet

IMAGE="${REGION}-docker.pkg.dev/${PROJECT_ID}/hrhelper/hrhelper:$(date +%s)"
echo "Building $IMAGE..."
docker build -t "$IMAGE" -f src/HRHelper/Dockerfile .

echo "Pushing $IMAGE..."
docker push "$IMAGE"

echo "Deploying Cloud Run service hrhelper..."
gcloud run deploy hrhelper \
  --image "$IMAGE" \
  --platform managed \
  --region "$REGION" \
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
