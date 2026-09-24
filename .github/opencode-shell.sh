#!/usr/bin/env bash

# SPDX-FileCopyrightText: 2026 Alper Çelik <alper@alper-celik.dev>
#
# SPDX-License-Identifier: AGPL-3.0-or-later OR Apache-2.0

exec nix develop "$(dirname "$(readlink -f "$0")")/.."#ci --command bash "$@"
