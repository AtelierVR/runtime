#!/usr/bin/env bash
set -euo pipefail

platform_config=".github/configs/build.yml"

json_escape() {
    local s="$1"
    s="${s//\\/\\\\}"
    s="${s//\"/\\\"}"
    printf '%s' "$s"
}

to_json_value() {
    local value="$1"
    if [[ "$value" =~ ^-?[0-9]+([.][0-9]+)?$ ]]; then
        printf '%s' "$value"
        return
    fi
    if [[ "$value" == "true" || "$value" == "false" || "$value" == "null" ]]; then
        printf '%s' "$value"
        return
    fi
    printf '"%s"' "$(json_escape "$value")"
}

append_pair() {
    local key="$1"
    local value="$2"
    local json_value
    json_value="$(to_json_value "$value")"
    if [ "$current_pair_count" -gt 0 ]; then
        current_item_json+=","
    fi
    current_item_json+="\"$(json_escape "$key")\":${json_value}"
    current_pair_count=$((current_pair_count + 1))
}

finalize_item() {
    if [ "$in_item" -eq 0 ]; then
        return
    fi

    if [ -z "$current_target" ] || [ -z "$current_runner" ]; then
        echo "::error::Each platform entry must define targetPlatform and runsOn in $platform_config"
        exit 1
    fi

    if [ -z "$current_display" ]; then
        current_display="$(echo "$current_target" | sed -E 's/^Standalone//; s/64$//')"
        if [ -z "$current_display" ]; then
            current_display="$current_target"
        fi
        append_pair "displayPlatform" "$current_display"
    fi

    platform_lower="$(echo "$current_display" | tr '[:upper:]' '[:lower:]' | sed -E 's#[^a-z0-9]+#-#g; s#-+#-#g; s#^-+##; s#-+$##')"
    if [ -z "$platform_lower" ]; then
        platform_lower="$(echo "$current_target" | tr '[:upper:]' '[:lower:]')"
    fi
    artifact_name="${repo_name}_${platform_lower}_v${version}${tag_suffix}.zip"
    append_pair "artifactName" "$artifact_name"

    if [ "$matrix_first" -eq 0 ]; then
        matrix_json+=","
        platforms_output+="; "
    fi
    matrix_first=0

    matrix_json+="{${current_item_json}}"
    estimated_download_url="https://github.com/${GITHUB_REPOSITORY}/releases/download/${release_tag}/${artifact_name}"
    platforms_table_rows+="| ${current_display} | ${current_target} | ${current_runner} | [Download](${estimated_download_url}) |"$'\n'
    platforms_output+="${current_display} (${current_target} on ${current_runner})"

    in_item=0
    current_item_json=""
    current_pair_count=0
    current_target=""
    current_runner=""
    current_display=""
}

parse_item_kv() {
    local kv="$1"
    if [[ ! "$kv" =~ ^([A-Za-z_][A-Za-z0-9_-]*)[[:space:]]*:[[:space:]]*(.*)$ ]]; then
        echo "::error::Invalid YAML key/value in $platform_config: $kv"
        exit 1
    fi

    local key="${BASH_REMATCH[1]}"
    local value="${BASH_REMATCH[2]}"

    value="${value%$'\r'}"
    value="${value%%#*}"
    value="$(echo "$value" | sed -E 's/[[:space:]]+$//')"

    if [[ "$value" =~ ^\"(.*)\"$ ]]; then
        value="${BASH_REMATCH[1]}"
    elif [[ "$value" =~ ^\'(.*)\'$ ]]; then
        value="${BASH_REMATCH[1]}"
    fi

    append_pair "$key" "$value"

    if [ "$key" = "targetPlatform" ]; then
        current_target="$value"
    elif [ "$key" = "runsOn" ]; then
        current_runner="$value"
    elif [ "$key" = "displayPlatform" ]; then
        current_display="$value"
    fi
}

if [ ! -f "$platform_config" ]; then
    echo "::error::Missing platform config: $platform_config"
    exit 1
fi

unity_version="$(grep "^m_EditorVersion:" ProjectSettings/ProjectVersion.txt | awk '{print $2}')"
if [ -z "$unity_version" ]; then
    echo "::error::Unable to read Unity version from ProjectSettings/ProjectVersion.txt"
    exit 1
fi

raw_channel="${GITHUB_HEAD_REF:-${GITHUB_REF_NAME}}"
channel="$(echo "$raw_channel" | tr '[:upper:]' '[:lower:]' | sed -E 's#[^a-z0-9._-]+#-#g; s#-+#-#g; s#^-+##; s#-+$##')"
if [ -z "$channel" ]; then
    channel="unknown"
fi

current_year="$(date -u +%G)"
year="$((current_year - 2000))"
week="$(date -u +%V)"
prefix="v${year}.${week}."
pattern="${prefix}*-${channel}"

max_build="$({
    git tag -l "$pattern" \
        | sed -E "s#^v${year}\\.${week}\\.([0-9]+)-${channel}$#\\1#" \
        | grep -E '^[0-9]+$' \
        | sort -n \
        | tail -1
} || true)"

if [ -z "$max_build" ]; then
    max_build=0
fi

build_num=$((max_build + 1))
version="${year}.${week}.${build_num}"
tag="${channel}"
release_name="Runtime ${version} (${channel})"
repo_name="${GITHUB_REPOSITORY#*/}"
if [ "$tag" = "main" ]; then
    release_tag="v${version}"
    tag_suffix=""
else
    release_tag="v${version}-${tag}"
    tag_suffix="-${tag}"
fi
artifact_pattern="${repo_name}_*_v${version}${tag_suffix}.zip"

if [ "$channel" = "main" ]; then
    prerelease="false"
else
    prerelease="true"
fi

matrix_json="["
matrix_first=1
platforms_output=""
platforms_table_rows=""

in_item=0
current_item_json=""
current_pair_count=0
current_target=""
current_runner=""
current_display=""

while IFS= read -r raw_line || [ -n "$raw_line" ]; do
    line="${raw_line%$'\r'}"
    if [ -z "${line//[[:space:]]/}" ]; then
        continue
    fi

    if [[ "$line" =~ ^[[:space:]]*# ]]; then
        continue
    fi

    if [[ "$line" =~ ^[[:space:]]*platforms:[[:space:]]*$ ]]; then
        continue
    fi

    if [[ "$line" =~ ^[[:space:]]*-[[:space:]]*(.*)$ ]]; then
        finalize_item
        in_item=1
        current_item_json=""
        current_pair_count=0
        current_target=""
        current_runner=""
        current_display=""

        inline_kv="${BASH_REMATCH[1]}"
        if [ -n "${inline_kv// /}" ]; then
            parse_item_kv "$inline_kv"
        fi
        continue
    fi

    if [ "$in_item" -eq 1 ] && [[ "$line" =~ ^[[:space:]]+([A-Za-z_][A-Za-z0-9_-]*)[[:space:]]*:[[:space:]]*(.*)$ ]]; then
        parse_item_kv "${BASH_REMATCH[1]}: ${BASH_REMATCH[2]}"
        continue
    fi

    echo "::error::Unsupported YAML line in $platform_config: $line"
    exit 1
done < "$platform_config"

finalize_item

matrix_json+="]"

if [ "$matrix_json" = "[]" ]; then
    echo "::error::No platforms configured in $platform_config"
    exit 1
fi

if [ -z "$platforms_output" ]; then
    echo "::error::No platform output generated from $platform_config"
    exit 1
fi

if [ -z "$platforms_table_rows" ]; then
    echo "::error::No platform table rows generated from $platform_config"
    exit 1
fi

{
    echo "unity_version=$unity_version"
    echo "channel=$channel"
    echo "version=$version"
    echo "tag=$tag"
    echo "release_tag=$release_tag"
    echo "release_name=$release_name"
    echo "platforms_json=$matrix_json"
    echo "platforms_output=$platforms_output"
    echo "artifact_pattern=$artifact_pattern"
    echo "prerelease=$prerelease"
} >> "$GITHUB_OUTPUT"

if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
    {
        echo "## Prepare Summary"
        echo "- Unity version: $unity_version"
        echo "- Tag: $tag"
        echo "- Version: $version"
        echo ""
        echo "### Platforms"
        echo "| Display | Target | Runner | Download |"
        echo "| --- | --- | --- | --- |"
        printf '%s' "$platforms_table_rows"
    } >> "$GITHUB_STEP_SUMMARY"
fi

echo "::notice::Prepare done | Unity=$unity_version | Tag=$tag | Version=$version | Platforms=$platforms_output"
