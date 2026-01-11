import os

import requests
import time
import xml.etree.ElementTree as ET
from xml.dom import minidom

APP_ID = 244850
TARGET_TOTAL = 2000
PER_PAGE = 100
MODS_PER_FILE = 200
API_KEY = os.environ['STEAM_API_KEY']


def fetch_top_mods():
    mods = []
    page = 1

    while len(mods) < TARGET_TOTAL:
        print(f"Fetching page {page} ({len(mods)}/{TARGET_TOTAL} collected)...")

        url = "https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/"
        params = {
            "key": API_KEY,
            "query_type": 0,  # Ranked by Vote
            "page": page,
            "numperpage": PER_PAGE,
            "appid": APP_ID,
            "requiredtags": ["mod"],
            "return_metadata": 1
        }

        try:
            response = requests.get(url, params=params, timeout=15)
            response.raise_for_status()
            data = response.json().get("response", {})

            items = data.get("publishedfiledetails", [])

            if not items:
                print("No results found or throttled. Waiting 5 minutes...")
                time.sleep(300)
                continue

            for item in items:
                if len(mods) < TARGET_TOTAL:
                    mods.append({
                        "title": item.get("title", "Unknown Mod"),
                        "id": item.get("publishedfileid")
                    })

            page += 1
            time.sleep(1)  # Polite delay

        except Exception as e:
            print(f"Request failed: {e}. Retrying in 1 minute...")
            time.sleep(60)

    return mods


def save_to_xml(mods):
    for i in range(0, len(mods), MODS_PER_FILE):
        chunk = mods[i: i + MODS_PER_FILE]

        root = ET.Element("Mods")

        for mod in chunk:
            mod_item = ET.SubElement(root, "ModItem", {
                "FriendlyName": mod["title"]
            })

            name = ET.SubElement(mod_item, "Name")
            name.text = f"{mod['id']}.sbm"

            file_id = ET.SubElement(mod_item, "PublishedFileId")
            file_id.text = str(mod["id"])

            service = ET.SubElement(mod_item, "PublishedServiceName")
            service.text = "Steam"

        # Pretty print and save
        xml_str = minidom.parseString(ET.tostring(root)).toprettyxml(indent="  ")
        filename = f"PopularMods{i // MODS_PER_FILE}.xml"

        with open(filename, "w", encoding="utf-8") as f:
            f.write(xml_str)

        print(f"Saved {filename}")


if __name__ == "__main__":
    all_mods = fetch_top_mods()
    save_to_xml(all_mods)
    print("\nProcessing complete.")