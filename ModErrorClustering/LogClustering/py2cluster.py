import json
import collections

def process_error_clusters(input_file, output_jsonl, output_txt):
    """
    Clusters errors by word count, sorts clusters by frequency,
    and writes to JSONL (with cluster IDs) and TXT (with visible headers).
    """
    items = []
    
    # 1. Load the JSONL file and keep track of original index
    try:
        with open(input_file, 'r', encoding='utf-8') as f:
            for i, line in enumerate(f):
                line = line.strip()
                if not line:
                    continue
                
                data = json.loads(line)
                error_text = data.get('error', '')
                # Similarity logic: same number of words split by space
                word_count = len(error_text.split())
                
                items.append({
                    'data': data,
                    'original_index': i,
                    'word_count': word_count
                })
    except FileNotFoundError:
        print(f"Error: {input_file} not found.")
        return

    # 2. Group items into clusters by word_count
    cluster_map = collections.defaultdict(list)
    for item in items:
        cluster_map[item['word_count']].append(item)

    # 3. Sort clusters by frequency (most items first)
    # Using word_count as a secondary sort key for stability
    sorted_cluster_keys = sorted(
        cluster_map.keys(), 
        key=lambda k: (len(cluster_map[k]), -k), 
        reverse=True
    )

    final_processed_items = []
    
    # 4. Prepare data for output with Cluster IDs
    for cluster_id, key in enumerate(sorted_cluster_keys):
        # Sort items inside the cluster by their original appearance order
        cluster_items = sorted(cluster_map[key], key=lambda x: x['original_index'])
        
        for item in cluster_items:
            # Add the cluster index to the JSON data
            item['data']['cluster_id'] = cluster_id
            final_processed_items.append((cluster_id, item['data']))

    # 5. Write to JSONL
    with open(output_jsonl, 'w', encoding='utf-8') as out_json:
        for _, data in final_processed_items:
            out_json.write(json.dumps(data) + '\n')

    # 6. Write to TXT with visible headers
    with open(output_txt, 'w', encoding='utf-8') as out_txt:
        current_cid = -1
        for cid, d in final_processed_items:
            # Check if we need to print a new cluster header
            if cid != current_cid:
                header = f"\n===========================================\n"
                header += f"CLUSTER #{cid}\n"
                header += f"===========================================\n"
                out_txt.write(header)
                current_cid = cid

            # Extraction based on schema
            mod_name = d.get('mod_name', 'Unknown Mod')
            mod_id = d.get('mod_id', 'Unknown ID')
            file_path = d.get('file', 'Unknown File')
            line = d.get('line', '0')
            col = d.get('column', '0')
            msg = d.get('error', '')

            out_txt.write(f"{mod_name} [{mod_id}]\n")
            out_txt.write(f"{file_path}:{line},{col}\n")
            out_txt.write(f"{msg}\n")
            out_txt.write("---\n")

if __name__ == "__main__":
    IN_FILE = "deduplicated_errors.jsonl"
    OUT_JSONL = "clustered_errors.jsonl"
    OUT_TXT = "clustered_errors.txt"
    
    process_error_clusters(IN_FILE, OUT_JSONL, OUT_TXT)
    print(f"Success. Files generated: {OUT_JSONL} and {OUT_TXT}")