# DigitWizard
Demo application enabling real time recognition of digits drawn utilizing Intel RealSense F200 Depth camera thanks to Keras and TensorFlowSharp.


### Overview

### Usage
* **Drawing:** user can draw digits on the canvas utilizing mouse or by performing a gestures in the air when F200 depth camera is turned on. All shapes (digits) have to be drawn with one curve only.
* **Cleaning the canvas:** each attempt to draw new shape cleares the canvas. Also a waving hand gesture clears it.
* **See the results:** after a sketch is completed, it is resized and given as input to the CNN model and bar chart indicating the percentage of recognition certainity for given digit is drawn under the canvas.

| Good sign recognized |  Bad sign - confused model |
|----------------|----------------|
| <p align="center"><img src="./src/good_example.png"></p> | <p align="center"><img src="./src/bad_example.png"></p>  |

### Requirements & remarks
* The solution was tested with **RealSense F200 depth camera**
* **TensorflowSharp 1.7.0** package needs to be installed from _nuget_ (I didn't put it to repo as it weights +200MB)

### References
[1] [*Intel® RealSenseTM SDK 10.0.26.0396*](http://registrationcenter.intel.com/irc_nas/9078/release_notes_realsense_sdk_2016_r2.pdf)  
[2] [*Keras MNIST-CNN demo*](https://github.com/keras-team/keras/blob/master/examples/mnist_cnn.py)  
[3] [*Keras models freezing script*](https://stackoverflow.com/questions/45466020/how-to-export-keras-h5-to-tensorflow-pb/45466355#45466355)  
[4] [*TensorFlowSharp: .NET bindings to the TensorFlow*](https://github.com/migueldeicaza/TensorFlowSharp)  
[5] [*Smart WPF bar charts*](http://dotnetvisio.blogspot.com/2013/08/wpf-create-custom-bar-chart-using-grid.html)
