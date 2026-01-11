import os
import re
import json
import glob

def parse_logs():
    # Regex patterns
    re_mod_name = re.compile(r"MOD_ERROR:\s*(.*)")
    re_mod_id = re.compile(r"Compilation of .*?(\d+)\.sbm_.* failed:")
    
    # Captures the path after '->' and before the '(line,col)'
    re_cs_error = re.compile(r"->\s+(.*)\((\d+),(\d+)\):\s*Error:\s*(.*)")

    # Regex to strip everything up to and including 'Data/Scripts/'
    re_path_prefix = re.compile(r".*Data[/\\]Scripts[/\\]", re.IGNORECASE)
    
    unique_errors = set()

    for log_path in glob.glob("*.log"):
        with open(log_path, 'r', encoding='utf-8', errors='ignore') as f:
            current_mod_name = "Unknown Mod"
            current_mod_id = "0000000000"

            for line in f:
                # Track Mod Name
                name_match = re_mod_name.search(line)
                if name_match:
                    current_mod_name = name_match.group(1).strip()
                    continue

                # Track Mod ID
                id_match = re_mod_id.search(line)
                if id_match:
                    current_mod_id = id_match.group(1).strip()
                    continue

                # Parse C# error
                cs_match = re_cs_error.search(line)
                if cs_match:
                    raw_path = cs_match.group(1).strip()
                    line_num = int(cs_match.group(2))
                    col_num = int(cs_match.group(3))
                    error_msg = cs_match.group(4).strip()

                    # Clean path: remove the log line artifacts and the base path
                    clean_path = re_path_prefix.sub("", raw_path)
                    rel_path = clean_path.replace("\\", "/")

                    # Store as a tuple for deduplication
                    # Order: (mod_name, rel_path, line, col, error_msg, mod_id)
                    error_record = (
                        current_mod_name,
                        rel_path,
                        line_num,
                        col_num,
                        error_msg,
                        current_mod_id
                    )
                    unique_errors.add(error_record)

    # Sort logic: (Name, Path, Line, Col, Msg)
    # err[0]=name, err[1]=path, err[2]=line, err[3]=col, err[4]=msg
    sorted_errors = sorted(list(unique_errors), key=lambda x: (x[0], x[1], x[2], x[3], x[4]))

    # --- JSONL Output ---
    with open("deduplicated_errors.jsonl", 'w', encoding='utf-8') as out_json:
        for err in sorted_errors:
            json_obj = {
                "mod_id": err[5],
                "mod_name": err[0],
                "file": err[1],
                "line": err[2],
                "column": err[3],
                "error": err[4]
            }
            out_json.write(json.dumps(json_obj) + "\n")

    # --- TXT Output ---
    with open("deduplicated_errors.txt", 'w', encoding='utf-8') as out_txt:
        for err in sorted_errors:
            mod_name, file_path, line, col, msg, mod_id = err
            out_txt.write(f"{mod_name} [{mod_id}]\n")
            out_txt.write(f"{file_path}:{line},{col}\n")
            out_txt.write(f"{msg}\n")
            out_txt.write("---\n")

    print(f"Success: Processed {len(unique_errors)} unique errors.")
    print("Files created: deduplicated_errors.jsonl, deduplicated_errors.txt")

if __name__ == "__main__":
    parse_logs()