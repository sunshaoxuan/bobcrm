
import xml.etree.ElementTree as ET
import sys
import os

def parse_cobertura(file_path):
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
    except Exception as e:
        print(f"Error parsing XML: {e}")
        return

    # Calculate overall coverage
    total_lines = 0
    covered_lines = 0
    
    file_stats = []

    packages = root.find('packages')
    if packages is None:
        return

    for package in packages.findall('package'):
        for classes in package.findall('classes'):
            for cls in classes.findall('class'):
                filename = cls.get('filename')
                if not filename or "BobCrm.Api" not in filename or "Tests" in filename:
                    continue
                
                lines = cls.find('lines')
                if lines is None:
                    continue
                
                l_total = 0
                l_covered = 0
                
                for line in lines.findall('line'):
                    l_total += 1
                    if int(line.get('hits', 0)) > 0:
                        l_covered += 1
                
                total_lines += l_total
                covered_lines += l_covered
                
                if l_total > 0:
                    coverage = (l_covered / l_total) * 100
                    file_stats.append({
                        'file': os.path.basename(filename),
                        'total': l_total,
                        'covered': l_covered,
                        'uncovered': l_total - l_covered,
                        'percent': coverage
                    })

    # Sort by uncovered lines desc
    file_stats.sort(key=lambda x: x['uncovered'], reverse=True)

    print(f"Overall Line Coverage: {((covered_lines/total_lines)*100):.2f}% ({covered_lines}/{total_lines})")
    print("-" * 60)
    print(f"{'File':<40} | {'Uncovered':<10} | {'Coverage':<10}")
    print("-" * 60)
    
    for stat in file_stats[:15]:
        print(f"{stat['file']:<40} | {stat['uncovered']:<10} | {stat['percent']:.1f}%")

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python parse_coverage.py <path_to_xml>")
    else:
        parse_cobertura(sys.argv[1])
