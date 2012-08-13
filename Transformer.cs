using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using TheBallCore_v1_0;
using TB=TheBallCore_v1_0;
using OP=Operation_v1_0;
using VariableTypeState = Operation_v1_0.VariableTypeState;

namespace TheBallCoreToOperationTRANS
{
    public class Transformer
    {
        T LoadXml<T>(string xmlFileName)
        {
            using (FileStream fStream = File.OpenRead(xmlFileName))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                T result = (T)serializer.Deserialize(fStream);
                fStream.Close();
                return result;
            }
        }



	    public Tuple<string, string>[] GetGeneratorContent(params string[] xmlFileNames)
	    {
            List<Tuple<string, string>> result = new List<Tuple<string, string>>();
            foreach(string xmlFileName in xmlFileNames)
            {
                TB.TheBallCoreAbstractionType fromAbs = LoadXml<TB.TheBallCoreAbstractionType>(xmlFileName);
                OP.OperationAbstractionType toAbs = TransformAbstraction(fromAbs);
                string xmlContent = WriteToXmlString(toAbs);
                FileInfo fInfo = new FileInfo(xmlFileName);
                string contentFileName = "OperationAbstractionFrom" + fInfo.Name;
                result.Add(Tuple.Create(contentFileName, xmlContent));
            }
	        return result.ToArray();
	    }

        private string WriteToXmlString(OP.OperationAbstractionType toAbs)
        {
            XmlSerializer serializer = new XmlSerializer(toAbs.GetType());
            MemoryStream memoryStream = new MemoryStream();
            serializer.Serialize(memoryStream, toAbs);
            byte[] data = memoryStream.ToArray();
            string result = System.Text.Encoding.UTF8.GetString(data);
            return result;
        }

        public static OP.OperationAbstractionType TransformAbstraction(TB.TheBallCoreAbstractionType fromAbs)
        {
            OP.OperationAbstractionType toAbs;
            toAbs = new OP.OperationAbstractionType()
            {
                Operations = new OP.OperationsType()
                {
                    codeNamespace = fromAbs.InstanceOfTheBall.semanticDomainName,
                    Operation = fromAbs.InstanceOfTheBall.Operations.Select(tbOp => GetOPOperation(tbOp)).ToArray(),
                },
            };
            return toAbs;
        }

        private static OP.OperationType GetOPOperation(TB.OperationType tbOper)
        {
            OP.OperationType op = new OP.OperationType()
             {
                 isRootOperation = false,
                 name = tbOper.name,
             };
            if(tbOper.Parameters != null)
            {
                op.Parameters = new OP.ParametersType()
                                    {
                                        Parameter = tbOper.Parameters.Parameter.Select(GetOPVariable).ToArray(),
                                    };
            }
            if(tbOper.Execution != null)
            {
                op.Execution = new OP.ExecutionType()
                                   {
                                       SequentialExecution =
                                           tbOper.Execution.SequentialExecution.Select(GetOPExecItem).ToArray(),
                                   };
            }
            if(tbOper.OperationSpec != null)
            {
                op.OperationSpec = new OP.OperationSpecType()
                                       {
                                           Description = tbOper.OperationSpec.Description,
                                           // TODO: Requirements
                                       };
            }
            if(tbOper.OperationReturnValues != null)
            {
                op.OperationReturnValues =
                    new OP.OperationReturnValuesType()
                        {
                            ReturnValue = tbOper.OperationReturnValues.ReturnValue.Select(GetOPVariable).ToArray(),
                            Parameter = tbOper.OperationReturnValues.Parameter.Select(GetOPTarget).ToArray(),
                            Target = tbOper.OperationReturnValues.Target.Select(GetOPTarget).ToArray(),
                        };
            }
            return op;
        }

        private static OP.TargetType GetOPTarget(TargetType tbTarget)
        {
            OP.TargetType target = new OP.TargetType()
                                       {
                                           name = tbTarget.name
                                       };
            return target;
        }

        private static OP.VariableType GetOPVariable(VariableType tbVariable)
        {
            OP.VariableType variable = new OP.VariableType()
                                           {
                                               name = tbVariable.name,
                                               dataType = tbVariable.dataType,
                                               designDesc = tbVariable.designDesc,
                                               state = GetOPVariableTypeState(tbVariable.state)
                                           };
            return variable;
        }

        private static OP.VariableTypeState GetOPVariableTypeState(TB.VariableTypeState state)
        {
            return (OP.VariableTypeState) Enum.Parse(typeof(OP.VariableTypeState), state.ToString());
        }

        private static object GetOPExecItem(object tbExecItem)
        {
            TB.MethodExecuteType methodExec = tbExecItem as TB.MethodExecuteType;
            TB.OperationExecuteType operationExec = tbExecItem as TB.OperationExecuteType;
            TB.TargetDefinitionType targetDef = tbExecItem as TB.TargetDefinitionType;
            if (methodExec != null)
                return GetOPMethodExec(methodExec);
            else if (operationExec != null)
                return GetOPOperationExec(operationExec);
            else if (targetDef != null)
                return GetOPTargetDef(targetDef);
            else
                throw new NotSupportedException("Operation exec item transformation from TB => OP missing for type: " + tbExecItem.GetType().Name);
        }

        private static OP.TargetDefinitionType GetOPTargetDef(TargetDefinitionType tbTarget)
        {
            OP.TargetDefinitionType opTarget = new OP.TargetDefinitionType()
                                                   {
                                                       name = tbTarget.name,
                                                       dataType = tbTarget.dataType,
                                                       designDesc = tbTarget.designDesc,
                                                       state = GetOPVariableTypeState(tbTarget.state),
                                                       Parameter = (tbTarget.Parameter ?? new TargetType[0]).Select(GetOPTarget).ToArray(),
                                                       Target = (tbTarget.Target ?? new TargetType[0]).Select(GetOPTarget).ToArray()
                                                   };
            return opTarget;
        }

        private static OP.OperationExecuteType GetOPOperationExec(OperationExecuteType tbOperExec)
        {
            OP.OperationExecuteType opExec = new OP.OperationExecuteType()
                                                 {
                                                     name = tbOperExec.name,
                                                     designDesc = tbOperExec.designDesc,
                                                     Parameter = (tbOperExec.Parameter ?? new TargetType[0]).Select(GetOPTarget).ToArray(),
                                                     Target = (tbOperExec.Target ?? new TargetType[0]).Select(GetOPTarget).ToArray(),
                                                     ReturnValue = (tbOperExec.ReturnValue ?? new VariableType[0]).Select(GetOPVariable).ToArray(),
                                                     state = GetOPVariableTypeState(tbOperExec.state),
                                                     targetOperationName = tbOperExec.targetOperationName,
                                                 };
            return opExec;
        }

        private static OP.MethodExecuteType GetOPMethodExec(MethodExecuteType tbMethodExec)
        {
            OP.MethodExecuteType methodExec = new OP.MethodExecuteType()
                                                  {
                                                      name = tbMethodExec.name,
                                                      designDesc = tbMethodExec.designDesc,
                                                      Parameter = (tbMethodExec.Parameter ?? new TargetType[0]).Select(GetOPTarget).ToArray(),
                                                      Target = (tbMethodExec.Target ?? new TargetType[0]).Select(GetOPTarget).ToArray(),
                                                      ReturnValue = (tbMethodExec.ReturnValue ?? new VariableType[0]) .Select(GetOPVariable).ToArray(),
                                                      state = GetOPVariableTypeState(tbMethodExec.state)
                                                  };
            return methodExec;
        }
    }
}
