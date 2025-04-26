#!/usr/bin/env python3
import os
import re
import sys
from collections import defaultdict
from dataclasses import dataclass
from typing import List, Dict, Set, Pattern, Tuple

@dataclass
class Issue:
    filepath: str
    line_number: int
    issue_type: str
    description: str
    severity: str  # high, medium, low

def scan_file(filepath: str) -> List[Issue]:
    """Scan a file for potential code issues."""
    issues = []
    
    if not os.path.exists(filepath) or not os.path.isfile(filepath):
        return issues
        
    # Skip files that should be ignored
    if any(part in filepath for part in ['obj/', 'bin/', '.vs/', 'node_modules/']):
        return issues
    
    # Only analyze certain file types
    extension = os.path.splitext(filepath)[1].lower()
    if extension not in ['.cs', '.py', '.js', '.json', '.xml', '.csproj']:
        return issues
    
    # Patterns to check (pattern, issue_type, description, severity)
    patterns = [
        # C# issues
        (r'throw\s+new\s+Exception\(', 'exception_usage', 'Using generic Exception instead of specific exception type', 'medium'),
        (r'catch\s*\(\s*Exception', 'exception_handling', 'Catching generic Exception', 'medium'),
        (r'\.Result;', 'async_antipattern', 'Using .Result instead of await', 'high'),
        (r'\.Wait\(\);', 'async_antipattern', 'Using .Wait() instead of await', 'high'),
        (r'Console\.Write', 'logging', 'Using Console.Write instead of proper logging', 'medium'),
        (r'string\.Format', 'string_formatting', 'Using string.Format instead of string interpolation', 'low'),
        (r'new\s+List<.*>\(\)', 'collections', 'Consider using collection initializers', 'low'),
        (r'\/\/\s*TODO', 'todo', 'TODO comment found', 'low'),
        (r'\/\/\s*HACK', 'hack', 'HACK comment found', 'medium'),
        (r'\/\/\s*FIXME', 'fixme', 'FIXME comment found', 'high'),
        (r'(?:public|protected|private)\s+(?:readonly\s+)?(?!readonly)(?!const)[A-Z]', 'naming', 'Public field not following naming conventions', 'medium'),
        
        # Nullable reference types issues
        (r'=\s*null!;', 'null_safety', 'Using null-forgiving operator (!)', 'medium'),
        (r'=\s*default!;', 'null_safety', 'Using null-forgiving operator (!)', 'medium'),
        (r'\?\?\s*throw', 'null_coalescing', 'Using ?? throw pattern', 'low'),
        
        # XML doc issues
        (r'<summary>\s*</summary>', 'documentation', 'Empty summary documentation', 'low'),
        (r'<param name="[^"]*">\s*</param>', 'documentation', 'Empty parameter documentation', 'low'),
        
        # Connection string issues
        (r'(?i)(?:password|pwd)=[^;]*;', 'security', 'Possible hardcoded credentials', 'high'),
        (r'(?i)(?:User ID|uid)=[^;]*;.*(?:password|pwd)=[^;]*;', 'security', 'Possible hardcoded credentials', 'high'),
        (r'(?i)mongodb(?:://|\+srv://)[^@]*:[^@]*@', 'security', 'Possible hardcoded MongoDB credentials', 'high'),
        
        # Dependency injection issues
        (r'new\s+[A-Z][A-Za-z0-9]*Service\(', 'di', 'Creating service directly instead of using DI', 'medium'),
        (r'new\s+[A-Z][A-Za-z0-9]*Repository\(', 'di', 'Creating repository directly instead of using DI', 'medium'),
        
        # Async issues
        (r'async\s+.*\{\s*[^}]*return\s+[^\.]*[^awaitTskAyc]+;', 'async_without_await', 'Async method without await', 'medium'),
        
        # Configuration issues
        (r'"(?:ConnectionString|ApiKey|Secret|Password)":\s*"[^"]{8,}"', 'security', 'Possible hardcoded secret in JSON configuration', 'high'),
    ]
    
    compiled_patterns = [(re.compile(pattern), issue_type, description, severity) for pattern, issue_type, description, severity in patterns]
    
    with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
        for i, line in enumerate(f):
            line_number = i + 1
            for pattern, issue_type, description, severity in compiled_patterns:
                if pattern.search(line):
                    issues.append(Issue(
                        filepath=filepath,
                        line_number=line_number,
                        issue_type=issue_type,
                        description=description,
                        severity=severity
                    ))
    
    return issues

def scan_directory(directory: str) -> List[Issue]:
    """Recursively scan a directory for code issues."""
    all_issues = []
    
    for root, dirs, files in os.walk(directory):
        # Skip directories that should be ignored
        dirs[:] = [d for d in dirs if d not in ['obj', 'bin', '.vs', 'node_modules']]
        
        for file in files:
            filepath = os.path.join(root, file)
            issues = scan_file(filepath)
            all_issues.extend(issues)
    
    return all_issues

def group_issues_by_type(issues: List[Issue]) -> Dict[str, List[Issue]]:
    """Group issues by their type."""
    grouped = defaultdict(list)
    for issue in issues:
        grouped[issue.issue_type].append(issue)
    return grouped

def group_issues_by_severity(issues: List[Issue]) -> Dict[str, List[Issue]]:
    """Group issues by their severity."""
    grouped = defaultdict(list)
    for issue in issues:
        grouped[issue.severity].append(issue)
    return grouped

def group_issues_by_file(issues: List[Issue]) -> Dict[str, List[Issue]]:
    """Group issues by their filepath."""
    grouped = defaultdict(list)
    for issue in issues:
        grouped[issue.filepath].append(issue)
    return grouped

def print_issues_summary(issues: List[Issue]):
    """Print a summary of the issues found."""
    if not issues:
        print("No issues found!")
        return
    
    print(f"Found {len(issues)} issues:")
    
    # Print by severity
    by_severity = group_issues_by_severity(issues)
    print("\nBy Severity:")
    for severity in ['high', 'medium', 'low']:
        if severity in by_severity:
            print(f"  {severity.upper()}: {len(by_severity[severity])}")
    
    # Print by issue type
    by_type = group_issues_by_type(issues)
    print("\nBy Issue Type:")
    for issue_type, issues_of_type in sorted(by_type.items(), key=lambda x: len(x[1]), reverse=True):
        print(f"  {issue_type}: {len(issues_of_type)}")
    
    # Print detail of high severity issues
    if 'high' in by_severity:
        print("\nHigh Severity Issues:")
        for i, issue in enumerate(by_severity['high']):
            if i >= 10:  # Limit to first 10 
                print(f"  ... and {len(by_severity['high']) - 10} more")
                break
            print(f"  {issue.filepath}:{issue.line_number} - {issue.description}")
    
    # Print files with most issues
    by_file = group_issues_by_file(issues)
    print("\nFiles with Most Issues:")
    for filepath, file_issues in sorted(by_file.items(), key=lambda x: len(x[1]), reverse=True)[:10]:
        print(f"  {filepath}: {len(file_issues)}")

def main():
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <directory>")
        sys.exit(1)
    
    directory = sys.argv[1]
    if not os.path.isdir(directory):
        print(f"Error: {directory} is not a directory")
        sys.exit(1)
    
    issues = scan_directory(directory)
    print_issues_summary(issues)

if __name__ == "__main__":
    main()