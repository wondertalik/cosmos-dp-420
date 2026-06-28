#!/bin/bash

#
# C# Code Formatter Script
# Formats C# projects using ReSharper Command Line Tools.
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
PROJECTS=""
FILES=""
SOLUTION_PATH=""
INCLUDE_GENERATED=false
VERBOSE=false
PROFILE="Built-in: Reformat & Apply Syntax Style"

print_color() {
    local color=$1
    local message=$2
    printf "${color}${message}${NC}\n"
}

show_usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Format C# code using ReSharper Command Line Tools."
    echo ""
    echo "Options:"
    echo "  -p, --projects PROJECTS    Comma-separated project names (or partial names)"
    echo "  -f, --files FILES          Comma-separated file paths"
    echo "  -s, --solution PATH        Path to .sln or .slnx (auto-detected if omitted)"
    echo "      --profile PROFILE      ReSharper cleanup profile"
    echo "  -g, --include-generated    Include generated files"
    echo "  -v, --verbose              Enable verbose output"
    echo "  -h, --help                 Show this help"
    echo ""
    echo "Examples:"
    echo "  $0"
    echo "  $0 -p \"ProjectA,ProjectB\""
    echo "  $0 -f \"src/ProjectA/Program.cs,src/ProjectB/Service.cs\""
    echo "  $0 -s path/to/solution.slnx --profile \"Built-in: Full Cleanup\""
}

require_option_value() {
    local option_name=$1
    local option_value=${2-}

    if [[ -z "$option_value" || "$option_value" == -* ]]; then
        print_color $RED "ERROR: $option_name requires a value."
        show_usage
        exit 1
    fi
}

while [[ $# -gt 0 ]]; do
    case $1 in
        -p|--projects)
            require_option_value "$1" "${2-}"
            PROJECTS="$2"
            shift 2
            ;;
        -f|--files)
            require_option_value "$1" "${2-}"
            FILES="$2"
            shift 2
            ;;
        -s|--solution)
            require_option_value "$1" "${2-}"
            SOLUTION_PATH="$2"
            shift 2
            ;;
        --profile)
            require_option_value "$1" "${2-}"
            PROFILE="$2"
            shift 2
            ;;
        -g|--include-generated)
            INCLUDE_GENERATED=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            show_usage
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            show_usage
            exit 1
            ;;
    esac
done

find_solution_file() {
    if [[ -n "$SOLUTION_PATH" ]]; then
        if [[ -f "$SOLUTION_PATH" ]]; then
            echo "$SOLUTION_PATH"
            return 0
        fi
        print_color $RED "ERROR: Solution path not found: $SOLUTION_PATH"
        exit 1
    fi

    local solution_file

    solution_file=$(find . -name "*.slnx" -type f | head -n 1)
    if [[ -n "$solution_file" ]]; then
        echo "$solution_file"
        return 0
    fi

    solution_file=$(find . -name "*.sln" -type f | head -n 1)
    if [[ -n "$solution_file" ]]; then
        echo "$solution_file"
        return 0
    fi

    return 1
}

get_csharp_projects_from_sln() {
    local solution_file=$1
    local solution_dir
    solution_dir=$(dirname "$solution_file")

    grep -E 'Project.*\.csproj' "$solution_file" | while IFS= read -r line; do
        local project_name
        local project_path

        project_name=$(echo "$line" | sed -n 's/.*= "\([^"]*\)".*/\1/p')
        project_path=$(echo "$line" | sed -n 's/.*", "\([^"]*\.csproj\)".*/\1/p')

        if [[ -n "$project_name" && -n "$project_path" ]]; then
            project_path=$(echo "$project_path" | sed 's|\\|/|g')
            local full_path="$solution_dir/$project_path"
            if [[ -f "$full_path" ]]; then
                echo "$project_name|$full_path|$project_path"
            fi
        fi
    done
}

get_csharp_projects_from_slnx() {
    local solution_file=$1
    local solution_dir
    solution_dir=$(dirname "$solution_file")

    grep -E '<Project Path="[^"]*\.csproj"' "$solution_file" | while IFS= read -r line; do
        local project_path
        local project_name

        project_path=$(echo "$line" | sed -n 's/.*Path="\([^"]*\.csproj\)".*/\1/p')
        project_path=$(echo "$project_path" | sed 's|\\|/|g')
        project_name=$(basename "$project_path" .csproj)

        if [[ -n "$project_path" ]]; then
            local full_path="$solution_dir/$project_path"
            if [[ -f "$full_path" ]]; then
                echo "$project_name|$full_path|$project_path"
            fi
        fi
    done
}

get_csharp_projects_from_src() {
    find ./src -name "*.csproj" -type f 2>/dev/null | while IFS= read -r full_path; do
        local project_name
        local project_path
        project_name=$(basename "$full_path" .csproj)
        project_path=${full_path#./}
        echo "$project_name|$full_path|$project_path"
    done
}

get_csharp_projects() {
    local solution_file=$1
    local ext
    ext="${solution_file##*.}"

    if [[ "$ext" == "slnx" ]]; then
        get_csharp_projects_from_slnx "$solution_file"
        return 0
    fi

    get_csharp_projects_from_sln "$solution_file"
}

build_jb_args() {
    local target=$1
    local include_generated=$2
    local is_verbose=$3
    local profile=$4

    local args=("cleanupcode" "$target" "--profile=$profile")

    if [[ "$include_generated" == "true" ]]; then
        args+=("--include=*")
    else
        args+=("--exclude=**/*.Designer.cs;**/*.g.cs;**/*.g.i.cs;**/*.cshtml;**/bin/**;**/obj/**")
    fi

    if [[ "$is_verbose" == "true" ]]; then
        args+=("--verbosity=INFO")
    else
        args+=("--verbosity=WARN")
    fi

    printf '%s\n' "${args[@]}"
}

print_jb_command() {
    local args=("$@")
    local quoted="jb"
    local arg
    for arg in "${args[@]}"; do
        case "$arg" in
            --verbosity=*)
                quoted="$quoted $arg"
                ;;
            --profile=*|--exclude=*|--include=*)
                quoted="$quoted ${arg%%=*}=\"${arg#*=}\""
                ;;
            *)
                quoted="$quoted \"$arg\""
                ;;
        esac
    done
    print_color $CYAN "Command: $quoted"
}

format_files() {
    local files_list=$1
    local include_generated=$2
    local is_verbose=$3
    local profile=$4

    IFS=',' read -ra file_array <<< "$files_list"
    local failure_count=0

    local file_path
    for file_path in "${file_array[@]}"; do
        file_path=$(echo "$file_path" | xargs)
        if [[ ! -f "$file_path" ]]; then
            print_color $YELLOW "WARNING: File not found: $file_path"
            failure_count=$((failure_count + 1))
            continue
        fi

        local args=()
        while IFS= read -r arg; do
            args+=("$arg")
        done < <(build_jb_args "$file_path" "$include_generated" "$is_verbose" "$profile")

        print_jb_command "${args[@]}"
        print_color $GREEN "Formatting: $file_path"

        if [[ "$is_verbose" == "true" ]]; then
            if jb "${args[@]}"; then
                print_color $GREEN "SUCCESS: $file_path"
            else
                print_color $RED "FAILED: $file_path"
                failure_count=$((failure_count + 1))
            fi
        elif jb "${args[@]}" >/dev/null 2>&1; then
            print_color $GREEN "SUCCESS: $file_path"
        else
            print_color $RED "FAILED: $file_path"
            failure_count=$((failure_count + 1))
        fi
    done

    return $failure_count
}

format_project() {
    local project_name=$1
    local project_path=$2
    local include_generated=$3
    local is_verbose=$4
    local profile=$5

    local project_dir
    project_dir=$(dirname "$project_path")

    local args=()
    while IFS= read -r arg; do
        args+=("$arg")
    done < <(build_jb_args "$project_dir" "$include_generated" "$is_verbose" "$profile")

    print_jb_command "${args[@]}"
    print_color $GREEN "Formatting: $project_name"

    if [[ "$is_verbose" == "true" ]]; then
        if jb "${args[@]}"; then
            print_color $GREEN "SUCCESS: $project_name"
            return 0
        fi
    elif jb "${args[@]}" >/dev/null 2>&1; then
        print_color $GREEN "SUCCESS: $project_name"
        return 0
    fi

    print_color $RED "FAILED: $project_name"
    return 1
}

main() {
    print_color $CYAN "C# Code Formatter"
    print_color $CYAN "==================="

    if [[ -n "$FILES" ]]; then
        print_color $BLUE "Formatting individual files..."
        print_color $BLUE "Using profile: $PROFILE"
        format_files "$FILES" "$INCLUDE_GENERATED" "$VERBOSE" "$PROFILE"
        if [[ $? -eq 0 ]]; then
            print_color $GREEN "All files processed successfully."
            exit 0
        fi
        print_color $RED "Some files failed to process."
        exit 1
    fi

    local all_projects=()
    local solution_file=""

    if solution_file=$(find_solution_file); then
        print_color $BLUE "Using solution: $(basename "$solution_file")"
        while IFS= read -r line; do
            [[ -n "$line" ]] && all_projects+=("$line")
        done < <(get_csharp_projects "$solution_file")
    else
        print_color $YELLOW "No .sln/.slnx found; falling back to src/**/*.csproj discovery."
    fi

    if [[ ${#all_projects[@]} -eq 0 ]]; then
        while IFS= read -r line; do
            [[ -n "$line" ]] && all_projects+=("$line")
        done < <(get_csharp_projects_from_src)
    fi

    if [[ ${#all_projects[@]} -eq 0 ]]; then
        print_color $RED "ERROR: No C# projects found."
        exit 1
    fi

    print_color $BLUE "Using profile: $PROFILE"
    print_color $BLUE "Found ${#all_projects[@]} C# projects"

    local projects_to_format=()
    if [[ -n "$PROJECTS" ]]; then
        IFS=',' read -ra requested_projects <<< "$PROJECTS"
        local requested
        for requested in "${requested_projects[@]}"; do
            requested=$(echo "$requested" | xargs)
            local found=false
            local project_info
            for project_info in "${all_projects[@]}"; do
                IFS='|' read -r project_name project_path project_relative_path <<< "$project_info"
                if [[ "$project_name" == "$requested" ]] || \
                   [[ "$(basename "$project_path" .csproj)" == "$requested" ]] || \
                   [[ "$project_relative_path" == *"$requested"* ]]; then
                    projects_to_format+=("$project_info")
                    found=true
                    break
                fi
            done
            if [[ "$found" == "false" ]]; then
                print_color $YELLOW "WARNING: Project not found: $requested"
            fi
        done
    else
        projects_to_format=("${all_projects[@]}")
    fi

    if [[ ${#projects_to_format[@]} -eq 0 ]]; then
        print_color $RED "ERROR: No matching projects found to format."
        exit 1
    fi

    print_color $BLUE "Formatting ${#projects_to_format[@]} projects..."

    local success_count=0
    local failure_count=0
    local project_info
    for project_info in "${projects_to_format[@]}"; do
        IFS='|' read -r project_name project_path project_relative_path <<< "$project_info"
        if format_project "$project_name" "$project_path" "$INCLUDE_GENERATED" "$VERBOSE" "$PROFILE"; then
            success_count=$((success_count + 1))
        else
            failure_count=$((failure_count + 1))
        fi
    done

    echo ""
    print_color $CYAN "Summary:"
    print_color $CYAN "=========="
    print_color $GREEN "Successful: $success_count"
    if [[ $failure_count -gt 0 ]]; then
        print_color $RED "Failed: $failure_count"
    fi

    if [[ $failure_count -eq 0 ]]; then
        print_color $GREEN "All projects processed successfully."
        exit 0
    fi

    print_color $RED "Some projects failed to process."
    exit 1
}

if ! command -v jb >/dev/null 2>&1; then
    print_color $RED "ERROR: ReSharper Command Line Tools (jb) are not installed."
    print_color $YELLOW "TIP: Install with: dotnet tool install -g JetBrains.ReSharper.GlobalTools"
    exit 1
fi

main "$@"
