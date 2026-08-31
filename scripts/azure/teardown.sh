#!/usr/bin/env bash
# Deletes the DevAssist Azure resource group.
set -euo pipefail

RESOURCE_GROUP="rg-devassist-ai"
YES=false

while [[ $# -gt 0 ]]; do
  case "$1" in
    --resource-group) RESOURCE_GROUP="$2"; shift 2 ;;
    --yes) YES=true; shift ;;
    -h|--help)
      echo "Usage: $0 [--resource-group NAME] [--yes]"
      exit 0
      ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

if [[ "$YES" != true ]]; then
  echo "This will DELETE resource group '$RESOURCE_GROUP' and ALL resources inside it."
  read -r -p "Type the resource group name to confirm: " confirm
  if [[ "$confirm" != "$RESOURCE_GROUP" ]]; then
    echo "Aborted."
    exit 1
  fi
fi

echo "Deleting resource group $RESOURCE_GROUP..."
az group delete --name "$RESOURCE_GROUP" --yes --no-wait
echo "Delete initiated. Check: az group show --name $RESOURCE_GROUP"
