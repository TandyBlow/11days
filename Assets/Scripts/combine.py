import os

def combine_code_files(project_directory, file_extensions, output_file):
    """
    查找一个项目目录中所有指定后缀的代码文件，并将它们的内容合并到一个输出文件中。

    :param project_directory: 项目的根目录路径。
    :param file_extensions: 一个包含所需文件后缀的列表 (例如: ['.py', '.md'])。
    :param output_file: 输出文件的路径。
    """
    with open(output_file, 'w', encoding='utf-8') as outfile:
        for root, _, files in os.walk(project_directory):
            for file in files:
                if any(file.endswith(ext) for ext in file_extensions):
                    file_path = os.path.join(root, file)
                    try:
                        with open(file_path, 'r', encoding='utf-8') as infile:
                            outfile.write(f"--- Start of {file_path} ---\n\n")
                            outfile.write(infile.read())
                            outfile.write(f"\n\n--- End of {file_path} ---\n\n")
                    except Exception as e:
                        outfile.write(f"--- Error reading {file_path}: {e} ---\n\n")

# --- 使用示例 ---
# 设置你的项目目录
project_path = '.'  # '.' 代表当前目录

# 设置你想要包含的代码文件后缀
extensions_to_include = ['.cs'] 

# 设置输出文件名
output_filename = 'combined_project_code.txt'

# 调用函数
combine_code_files(project_path, extensions_to_include, output_filename)

print(f"所有指定的代码文件已成功汇总到 {output_filename}")
