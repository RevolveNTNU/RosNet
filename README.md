<div class="row">
  <div class="column">

  </div>
  <div class="column">

  </div>
</div>
<p float="left">
    <a href="https://revolve.no/"><img align=left src="https://raw.githubusercontent.com/RevolveNTNU/RosNet/86-improve-readme/.github/main/revolve.svg" width="30%" height="100"/></a>
    <a href="https://ros.org/"><img align=right margin src="https://raw.githubusercontent.com/RevolveNTNU/RosNet/86-improve-readme/.github/main/ros.png" height="100"/></a>
</p>
<br>
<br>
<br>
<br>
<br>

[![Build, run tests and generate report](https://github.com/RevolveNTNU/RosNet/actions/workflows/build_and_test.yml/badge.svg)](https://github.com/RevolveNTNU/RosNet/actions/workflows/build_and_test.yml)
[![Coverage Status](https://coveralls.io/repos/github/RevolveNTNU/RosNet/badge.svg?branch=86-improve-readme)](https://coveralls.io/github/RevolveNTNU/RosNet?branch=86-improve-readme)

# RosNet
## What is RosNet?
RosNet is a .NET library for deserializing rosbags version 2.0 to C#-objects.

## How to use the library:
<!-- User documentation-->
To use RosNet in a C#-based application: Install RosNet as a NuGet Package

### Using statement
```C#
using RosNet.DataModel;
```
### Convert a ROSBag to a C# object
```C#
var rosBag = new RosBag(path);
rosBag.Read();
```
### Obtain possible ROS topics and fields in the topics
```C#
Dictionary<string, List<string>> fieldNamesByTopic = rosBag.GetConnectionFields();
```

### Obtain list of tuples with timestamp and fieldvalue
```C#
List<(Time, FieldValue)> timeSeries = rosBag.GetTimeSeries(topic, fieldName);
```
### FieldValue

FieldValue is a custom data type with a 

- string name -  name of the field
- PrimitiveType DataType -  datatype of the value. PrimitiveType is an Enum with possible values: Bool, Int8, Uint8, Int16, Unit16, Int32, Uint32, Int64, Uint64, Float32, Float64, String, Time, Duration, Byte, Char and Array. The datatypes correspond to the [standard datatypes](https://wiki.ros.org/msg) in ROS messages and Array used for arrays of values.
- byte[] Value - is the value of the field

#### Conversion between PrimitiveType and C# types:
| PrimitiveType | C# type               |
| -----------   | -----------           |
| Bool          | bool                  |
| Byte          | sbyte                 |
| Char          | char                  |
| Duration      | RosNet.DataModel.Time |
| Float32       | float                 |
| Float64       | double                |
| Int8          | sbyte                 |
| Int16         | short                 |
| Int32         | int                   |
| Int64         | long                  |
| Time          | RosNet.DataModel.Time |
| Uint8         | byte                  |
| Uint16        | ushort                |
| Uint32        | uint                  |
| Uint64        | ulong                 |

#### ArrayFieldValue

ArrayFieldValue represents a fieldvalue that is an array of fieldvalues. The class inherits from FieldValue and has an additional list of fieldvalues.

### Time

Time is a custom data type with a uint called Secs and a uint called NSecs. The class is comparable and has the function ToDateTime() that returns the Time as a DateTime object.
## External dependencies
SharpZipLib used for decompression from bz2: http://icsharpcode.github.io/SharpZipLib/

## Credits
This library is made by the organization Revolve NTNU: https://www.revolve.no

Contributors:  
Henrik Hørlück Berg https://github.com/henrikhorluck  
Inge Grelland https://github.com/Kytzis  
Juni Sæther Skarpaas https://github.com/Juni-hub  
Lars van der Lee https://github.com/TheLarsinator  
Mikael Steenbuch https://github.com/mikaelste

<!-- License -->
