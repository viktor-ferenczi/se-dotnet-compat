import os
import shutil

script_dir = os.path.dirname(os.path.abspath(__file__))
template_dir = os.path.join(script_dir, "TemplateWorld")
xml_dir = script_dir

for i in range(10):
    xml_file = f"PopularMods{i}.xml"
    xml_path = os.path.join(xml_dir, xml_file)

    with open(xml_path, 'r', encoding='utf-8') as f:
        xml_content = f.read()

    # Extract the <Mods> section from XML
    start = xml_content.find('<Mods>')
    end = xml_content.find('</Mods>') + len('</Mods>')
    mods_content = xml_content[start:end]

    # Create new directory
    new_dir = os.path.join(script_dir, f"PopularMods{i}")
    if os.path.exists(new_dir):
        shutil.rmtree(new_dir)
    shutil.copytree(template_dir, new_dir)

    # Modify Sandbox.sbc
    sbc_path = os.path.join(new_dir, "Sandbox.sbc")
    with open(sbc_path, 'r', encoding='utf-8') as f:
        sbc_content = f.read()
    sbc_content = sbc_content.replace('<SessionName>Template</SessionName>', f'<SessionName>PopularMods{i}</SessionName>')
    with open(sbc_path, 'w', encoding='utf-8') as f:
        f.write(sbc_content)

    # Modify Sandbox_config.sbc
    config_path = os.path.join(new_dir, "Sandbox_config.sbc")
    with open(config_path, 'r', encoding='utf-8') as f:
        config_content = f.read()
    config_content = config_content.replace('<SessionName>Template</SessionName>', f'<SessionName>PopularMods{i}</SessionName>')
    # Replace the empty <Mods /> with the mods content
    config_content = config_content.replace('<Mods />', mods_content)
    with open(config_path, 'w', encoding='utf-8') as f:
        f.write(config_content)

print("Created test worlds")
