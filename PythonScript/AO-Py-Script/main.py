# Author: Michael Brochard
# GitHub username: nezcoupe
# Date: 12/14/2022

from image_tess_text import image_to_text
import copy
import json
import sys
import os
from pathlib import Path


class AnyExcept(Exception):
    """catch image broken exception with processing function"""
    pass


def main():
    """main script to accept image path from command line, process, analyze and return string of members
    contained in the image"""
    mem_list_copy = copy.deepcopy(mem_list)  # create a deep copy for mutation

    # copy member list to retain original names, replace 0s with o and 1s with l
    for n in range(0, len(mem_list_copy)):
        temp = [*mem_list_copy[n]]
        for i in range(0, len(temp)):
            if temp[i] == "0":
                temp[i] = "o"
            elif temp[i] == "1":
                temp[i] = "l"
        joined = "".join([str(char) for char in temp])
        mem_list_copy[n] = joined.lower()

    ind_list = []
    ret_str_list = []

    image_path = sys.argv[1]  # get command line argument from C#
    try:
        img_str = image_to_text(image_path)  # returns full string of names found in lower case

        for m in range(0, len(mem_list_copy)):  # find indices for names in original mem list
            if mem_list_copy[m] in img_str:
                ind_list.append(m)
        for n in range(0, len(ind_list)):  # build return list from strings in original mem list
            ret_str_list.append(mem_list[ind_list[n]])
        print(ret_str_list)
    except AnyExcept:
        print("Loot split not processed, please try again.")


# set new_path to the parent.parent.parent working directory, so we can access the discord bot \Temp folder
current_path = os.getcwd()
path = Path(current_path)
new_path = str(path.parent.parent.parent.absolute())

with open(new_path + r"\Temp\members.json", "r") as infile:
    mem_list = json.load(infile)

main()