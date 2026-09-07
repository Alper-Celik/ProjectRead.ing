#!/usr/bin/env bash
exec nix develop "$(dirname "$(readlink -f "$0")")/.."#ci --command bash "$@"
