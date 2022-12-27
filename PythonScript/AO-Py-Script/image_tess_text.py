# Author: Michael Brochard
# GitHub username: nezcoupe
# Date: 12/14/2022

import cv2
from PIL import Image
from PIL.Image import Resampling
from pytesseract import pytesseract
import os
from pathlib import Path
# import numpy as np


def image_to_text(image):
    """function that accepts as a single parameter an image containing text; returns a string containing the characters
    found in the image"""
    # path to tess; point tesseract
    current_path = os.getcwd()
    path = Path(current_path)
    new_path = str(path.parent.parent.parent.absolute())
    path_to_tess = r"C:\Program Files\Tesseract-OCR\tesseract.exe"
    pytesseract.tesseract_cmd = path_to_tess

    # file path
    inp_img = Image.open(image)

    # for cv2 manipulation
    # inp_img = cv2.imread(image)

    def set_newsize(_image):
        """triples dimensions on input, parameter is Image object"""
        # todo: conditional statements here to resize or crop large images

        length_x, width_y = _image.size

        if length_x > 2000:
            length_x = length_x // 3

        if length_x > 1000:
            length_x = length_x // 2

        if width_y > 1200:
            width_y = width_y // 2

        image_resized = _image.resize((length_x * 3, width_y * 3), Resampling.LANCZOS)
        image_resized.save(new_path + r"\Temp\newsize.png")

        return

    def remove_noise(_image):
        """"""
        return cv2.fastNlMeansDenoisingColored(_image, None, 10, 10, 7, 15)

    def get_grayscale(_image):
        """converts to grayscale, parameter is cv2 object"""
        return cv2.cvtColor(_image, cv2.COLOR_BGR2GRAY)

    def invert(_image):
        """inverts colors b-w, w-b, parameter is cv2 object"""
        return cv2.bitwise_not(_image)

    set_newsize(inp_img)
    img = cv2.imread(new_path + r"\Temp\newsize.png")
    no_noise = remove_noise(img)

    # uncomment to see preprocessing step
    # cv2.imwrite(new_path + r"\Temp\no_noise.png", no_noise)

    gray_img = get_grayscale(no_noise)

    # uncomment to see preprocessing step
    # cv2.imwrite(new_path + r"\Temp\grayscale.png", gray_img)

    # uncomment to see preprocessing step
    # inv_img = invert(gray_img)
    # cv2.imwrite(new_path + r"\Temp\invert.png", inv_img)

    temp_str = ""

    # range can be changed (increase to capture more, decrease to capture less, i = 127 is the sweet spot)
    for i in range(110, 140):
        thresh, im_bw = cv2.threshold(gray_img, i, 255, cv2.THRESH_BINARY)
        inv_img = invert(im_bw)
        if i == 127:
            cv2.imwrite(new_path + r"\Temp\threshold127.png", inv_img)
        output_str = pytesseract.image_to_string(inv_img, config="-c tessedit_char_whitelist=0123456789ABCDEFGHIJKLMNOP"
                                                                 "QRSTUVWXYZabcdefghijklmnopqrstuvwxyz").lower()
        temp_str += output_str
    return temp_str


