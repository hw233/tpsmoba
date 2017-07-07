# -*- coding: utf-8 -*-

import os
import xlrd
import sys
import hashlib
import json
import export_rules

is_warning_open = False
md5_data = {}


def read_md5(file_path):
    global md5_data
    try:
        with open(file_path, 'rb') as f:
            md5_data = json.load(f)
    except:
        print "No json file, now create"


def write_md5(file_path):
    with open(file_path, 'w') as f:
        json.dump(md5_data, f)


def calc_md5(filepath):
    with open(filepath,'rb') as f:
        md5obj = hashlib.md5()
        md5obj.update(f.read())
        hash_data = md5obj.hexdigest()
        return hash_data


# 计算row_index对应的excel列名称，row_index从1开始
def calc_excel_row(row_index):
    if row_index < 1:
        print "Error in calc_excel_row, wrong row index: ", row_index

    row_str = bytearray(20)
    itr = row_index
    idx = 0
    while itr != 0:
        last_chr = chr((itr - 1) % 26 + ord('A'))
        row_str[idx] = last_chr

        idx += 1
        itr = int((itr - 1) / 26)

    for i in range(0, int(idx / 2)):
        temp = row_str[i]
        row_str[i] = row_str[idx - 1 - i]
        row_str[idx - 1 - i] = temp
    return str(row_str)


def error_location(line, row, err_msg):
    if row:
        row_str = calc_excel_row(row)
        print "\nError in line " + str(line) + " row " + row_str + ":",
    else:
        print "\nError in line " + str(line) + ":",
    print err_msg


def warning_location(line, row, err_msg):
    if not is_warning_open:
        return

    if row:
        row_str = calc_excel_row(row)
        print "\nWarning in line " + str(line) + " row " + row_str + ":",
    else:
        print "\nWarning in line " + str(line) + ":",
    print err_msg
        

def safe_data_convert(data, type_name):
    is_match = True
    err_msg = ""
    new_data = None

    if type_name == "int":
        if data == "":
            data = 0
        try:
            new_data = str(int(data))
        except:
            is_match = False
            err_msg = "非int型数据: " + data
    elif type_name == "float":
        if data == "":
            data = 0
        try:
            new_data = str(float(data))
        except:
            is_match = False
            err_msg = "非float型数据: " + data
    elif type_name == "bool":
        if data == "":
            data = false
        try:
            new_data = str(bool(data)).lower()
        except:
            is_match = False
            err_msg = "非bool型数据: " + data
    elif type_name == "string":
        try:
            new_data = data.replace('"', '\\"')
            new_data = "\"" + new_data + "\""
        except:
            is_match = False
            err_msg = "非string型数据: " + data
    elif type_name == "list_int":
        if data == "":
            new_data = "["
        elif isinstance(data, str) or isinstance(data, unicode):
            data = data.replace('=', '|')
            split_str = data.split('|')
            for index, item in enumerate(split_str):
                try:
                    split_str[index] = str(int(item))
                    if new_data is None:
                        new_data = "["
                    new_data += split_str[index]
                    new_data += ", "
                except:
                    is_match = False
                    err_msg = "list_int必须为‘|’隔开的整数: " + data
                    break
            new_data = new_data[:-2]
        else:
            new_data = "["
            try:
                new_data += str(int(data))
            except:
                is_match = False
                err_msg = "list_int:非int型数据: " + data
        new_data += "]"
    elif type_name == "list_float":
        if data == "":
            new_data = "["
        elif isinstance(data, str) or isinstance(data, unicode):
            data = data.replace('=', '|')
            split_str = data.split('|')
            for index, item in enumerate(split_str):
                try:
                    split_str[index] = str(float(item))
                    if new_data is None:
                        new_data = "["
                    new_data += split_str[index]
                    new_data += ", "
                except:
                    is_match = False
                    err_msg = "list_int必须为‘|’隔开的浮点数: " + data
                    break
            new_data = new_data[:-2]
        else:
            new_data = "["
            try:
                new_data += str(float(data))
            except:
                is_match = False
                err_msg = "list_float:非float型数据: " + data
        new_data += "]"
    else:
        is_match = False
        err_msg = "错误的数据类型: " + type_name

    return new_data, is_match, err_msg


def read_excel_data(filename):
    print u"\n\n开始处理表: ",filename
    print u"处理读取页中..."
    game_data = {}
    excel_data = xlrd.open_workbook(filename)
    # 第1页映射了中文sheet名称和对应英文table名称
    table0 = excel_data.sheets()[0]

    # 读取第一页中的导航页中的所有字段，每个字段对应excel中的一张子表
    for table_index in xrange(table0.nrows):
        tablename_cn, tablename_en = table0.row_values(table_index)[0:2]
        if len(tablename_cn) == 0 or len(tablename_en) == 0:
            continue
        
        tablename_en = tablename_en.strip()
        try:
            table = excel_data.sheet_by_name(tablename_cn)
        except:
            #print u"读取sheet出错：" + tablename_cn
            error_location(table_index + 1, None, "读取sheet出错：" + tablename_cn)
            return -1

        game_data[tablename_en] = table
    return game_data


def write_to_lua(data, out_filename, in_filename=""):
    tab = "    "
    with open(out_filename, "w") as fout:
        file_str = "{ \n"
        for tablename_en, table in data.iteritems():
            print u"开始处理sheet ",tablename_en
            file_str += tab + "\"" + tablename_en + "\" : {\n"
            # 第一行是注释，第二行是对应的拼音字段, 第三行是类型
            try:
                desc_row = table.row_values(0)
                keys_row = table.row_values(1)
                type_row = table.row_values(2)
            except:
                err_msg = "第一行是注释，第二行是对应的拼音字段, 第三行是类型"
                error_location(1, None, err_msg)
                return -1
            
            for index in xrange(len(keys_row)):
                try:
                    type_row[index] = type_row[index].strip()
                    if len(keys_row[index]) != 0:
                        if str(type_row[index]) not in ["int", "float", "bool", "string", "list_int", "list_float"]:
                            err_msg = "类型必须为int, float, bool ,string, list_int, list_float之一 " + str(keys_row[index]) + ": " + str(
                                type_row[index]) + tab + desc_row[index]
                            error_location(3, index + 1, err_msg)
                            return -1
                        #file_str += tab * 2 + str(keys_row[index]) + ": " + str(type_row[index]) + tab + desc_row[index] + "\n"
                except:
                    err_msg = "类型必须为int, float, bool ,string, list_int, list_float之一 " + str(keys_row[index]) + ": " + str(
                        type_row[index]) + tab + desc_row[index]
                    error_location(3, index + 1, err_msg)
                    return -1

            id_flag = -1
            for i in xrange(len(keys_row)):
                if "ID" == str(keys_row[i]):
                    id_flag = i

            for row_index in xrange(3, table.nrows):
                row_data = table.row_values(row_index)
                if id_flag == -1:
                    table_key = row_index-2
                else:
                    table_key = row_data[id_flag]

                if isinstance(table_key, str) or isinstance(table_key, unicode):
                    table_key = "\"" + table_key + "\""
                else:
                    table_key, is_match, err_msg = safe_data_convert(table_key, "int")
                    table_key = "\"" + table_key + "\""
                    if not is_match:
                        error_location(row_index + 1, index + 1, err_msg)
                        return -1

                if table_key == "\"\"" or table_key == u"\"\"":
                    err_msg = "索引值为空，略过本行"
                    warning_location(row_index + 1, index + 1, err_msg)
                    continue

                file_str += tab * 2 + str(table_key) + " : { \n"

                for index in xrange(len(row_data)):
                    #print "---",row_data,tablename_en
                    key = keys_row[index]
                    if len(key) == 0:
                        continue
                    typee = type_row[index]
                    item = row_data[index]

                    item, is_match, err_msg = safe_data_convert(item,typee)
                    if not is_match:
                        error_location(row_index + 1, index + 1, err_msg)
                        return -1

                    file_str += tab * 3 + "\"" + str(key) + "\" : " + item + ",\n"
                file_str = file_str[:-2] + "\n"
                file_str += tab * 2 + "},\n"
            file_str = file_str[:-2] + "\n"
            file_str += tab + "},\n\n"
        file_str = file_str[:-3] + "\n}"
        fout.write(file_str)


def test_write():
    d = {"shuxing":{}}
    d["shuxing"][1] = {"DJ":100, "GWQD":200, "SM":300}
    d["shuxing"][2] = {"DJ": 400, "GWQD": 500, "SM": 500}
    d["shuxing"][3] = {"DJ": 700, "GWQD": 800, "SM": 900}
    write_to_lua(d, "zhandou")


def convert_excel_to_lua(in_filename, out_filename):
    if in_filename.split(".")[-1] != "xlsx":
        return

    data = read_excel_data(in_filename)
    if data == -1:
        return -1


    #write_to_lua(data, out_filename, in_filename)
    rst = write_to_lua(data, out_filename, in_filename)
    if rst == -1:
        return -1


def process_file(file_name, file_folder, out_file_path):
    file_path = file_folder + "\\" + file_name
    file_md5 = calc_md5(file_path)

    #如果md5没变且相应lua文件存在则不处理
    if file_name in md5_data.keys():
        if md5_data[file_name] == file_md5:
            if os.path.isfile(out_file_path):
                return
    md5_data[file_name] = file_md5

    rst = convert_excel_to_lua(file_path, out_file_path)
    if rst == -1:
        del md5_data[file_name]


def list_dir(folder, out_folder):
    """遍历所有的文件进行相关处理"""
    name_map = export_rules.name_map
    for file_name in name_map.keys():
        out_file_path = out_folder + "\\" + export_rules.name_map[file_name]
        file_name = file_name.decode("utf-8")
        process_file(file_name, folder, out_file_path)


if __name__ == "__main__":
    reload(sys)
    sys.setdefaultencoding('utf-8')

    data_folder = "..\\excel_files"
    lua_folder = "..\\lua_files"
    json_path = "..\\excel_files\\md5.json"

    read_md5(json_path)
    list_dir(data_folder, lua_folder)
    write_md5(json_path)
    os.system("pause")
