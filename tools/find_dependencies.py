#!/usr/bin/env python3
import os
import re
import xml.etree.ElementTree as ET
import sys
from typing import Dict, List, Set, Optional

def find_csproj_files(directory: str) -> List[str]:
    """Find all .csproj files in the given directory."""
    csproj_files = []
    for root, _, files in os.walk(directory):
        for file in files:
            if file.endswith('.csproj'):
                csproj_files.append(os.path.join(root, file))
    return csproj_files

def get_project_name(csproj_path: str) -> str:
    """Extract the project name from the .csproj file path."""
    return os.path.basename(csproj_path).replace('.csproj', '')

def extract_package_references(csproj_path: str) -> List[Dict[str, str]]:
    """Extract package references from a .csproj file."""
    try:
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        packages = []
        
        for package_ref in root.findall('.//PackageReference'):
            package = {
                'Include': package_ref.get('Include'),
                'Version': package_ref.get('Version')
            }
            packages.append(package)
        
        return packages
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}")
        return []

def extract_project_references(csproj_path: str) -> List[str]:
    """Extract project references from a .csproj file."""
    try:
        tree = ET.parse(csproj_path)
        root = tree.getroot()
        project_refs = []
        
        for project_ref in root.findall('.//ProjectReference'):
            include = project_ref.get('Include')
            if include:
                # Extract project name from path
                project_name = os.path.basename(include).replace('.csproj', '')
                project_refs.append(project_name)
        
        return project_refs
    except Exception as e:
        print(f"Error parsing {csproj_path}: {e}")
        return []

def find_using_statements(cs_files: List[str]) -> Dict[str, Set[str]]:
    """Find using statements in C# files."""
    using_statements = {}
    
    using_regex = re.compile(r'^\s*using\s+([^;]+);')
    
    for cs_file in cs_files:
        if not os.path.exists(cs_file):
            continue
            
        file_usings = set()
        try:
            with open(cs_file, 'r', encoding='utf-8', errors='ignore') as f:
                for line in f:
                    match = using_regex.match(line)
                    if match:
                        using = match.group(1).strip()
                        # Skip using static
                        if not using.startswith('static '):
                            file_usings.add(using)
        except Exception as e:
            print(f"Error reading {cs_file}: {e}")
            
        using_statements[cs_file] = file_usings
    
    return using_statements

def find_cs_files_in_project(project_dir: str) -> List[str]:
    """Find all .cs files in a project directory."""
    cs_files = []
    for root, _, files in os.walk(project_dir):
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    return cs_files

def analyze_dependencies(directory: str):
    """Analyze dependencies in projects."""
    csproj_files = find_csproj_files(directory)
    
    # Map from project name to package references
    project_packages = {}
    # Map from project name to project references
    project_references = {}
    # Map from project name to directory
    project_directories = {}
    
    for csproj_path in csproj_files:
        project_name = get_project_name(csproj_path)
        project_dir = os.path.dirname(csproj_path)
        project_directories[project_name] = project_dir
        
        # Extract package references
        package_refs = extract_package_references(csproj_path)
        project_packages[project_name] = package_refs
        
        # Extract project references
        proj_refs = extract_project_references(csproj_path)
        project_references[project_name] = proj_refs
    
    # Analyze each project
    for project_name, project_dir in project_directories.items():
        print(f"\nAnalyzing project: {project_name}")
        
        # Package dependencies
        packages = project_packages.get(project_name, [])
        if packages:
            print(f"  Package Dependencies ({len(packages)}):")
            for package in packages:
                print(f"    - {package['Include']} (v{package['Version']})")
        else:
            print("  No NuGet package dependencies")
        
        # Project dependencies
        proj_refs = project_references.get(project_name, [])
        if proj_refs:
            print(f"  Project References ({len(proj_refs)}):")
            for ref in proj_refs:
                print(f"    - {ref}")
        else:
            print("  No project references")
        
        # Find all .cs files in the project
        cs_files = find_cs_files_in_project(project_dir)
        
        # Extract using statements
        using_statements = find_using_statements(cs_files)
        
        # Flatten all using statements
        all_usings = set()
        for file_usings in using_statements.values():
            all_usings.update(file_usings)
        
        # Group by package
        print("  Top-level namespaces used:")
        grouped_usings = {}
        for using in all_usings:
            top_namespace = using.split('.')[0]
            if top_namespace not in grouped_usings:
                grouped_usings[top_namespace] = []
            grouped_usings[top_namespace].append(using)
        
        # Print grouped usings
        for ns, usings in sorted(grouped_usings.items()):
            print(f"    - {ns} ({len(usings)})")

def main():
    if len(sys.argv) != 2:
        print(f"Usage: {sys.argv[0]} <directory>")
        sys.exit(1)
    
    directory = sys.argv[1]
    if not os.path.isdir(directory):
        print(f"Error: {directory} is not a directory")
        sys.exit(1)
    
    analyze_dependencies(directory)

if __name__ == "__main__":
    main()