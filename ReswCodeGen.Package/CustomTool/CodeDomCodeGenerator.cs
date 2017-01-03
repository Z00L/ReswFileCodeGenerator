using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.CSharp;

namespace ReswCodeGen.CustomTool
{
	public class CodeDomCodeGenerator : CodeGenerator, IDisposable
	{
		private readonly TypeAttributes? _classAccessibility;
		//private readonly VisualStudioVersion _visualStudioVersion;
		private readonly string _className;
		private readonly CodeDomProvider _provider;

		public CodeDomCodeGenerator(IResourceParser resourceParser,
									string className,
									string defaultNamespace,
									CodeDomProvider codeDomProvider = null,
									TypeAttributes? classAccessibility = null,
									VisualStudioVersion visualStudioVersion = default(VisualStudioVersion))
			: base(resourceParser, defaultNamespace)
		{
			_className = className;
			_classAccessibility = classAccessibility;
			//_visualStudioVersion = visualStudioVersion;
			_provider = codeDomProvider ?? new CSharpCodeProvider();
		}

		public override string GenerateCode()
		{
			var rootClass = GenerateRootClass(_className, _classAccessibility ?? TypeAttributes.Public);
			var rootMembers = GenerateRootMembers(rootClass.Name).ToArray();
			rootClass.Members.AddRange(rootMembers);

			var codeNamespace = new CodeNamespace(Namespace);
			// using System;
			codeNamespace.Imports.Add(new CodeNamespaceImport(typeof(object).Namespace));
			// using System.Reflection;
			codeNamespace.Imports.Add(new CodeNamespaceImport(typeof(Assembly).Namespace));
			// using System.Threading;
			codeNamespace.Imports.Add(new CodeNamespaceImport(typeof(CancellationTokenSource).Namespace));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Windows.ApplicationModel.Core"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Windows.ApplicationModel.Resources.Core"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Windows.UI.Core"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Windows.UI.Xaml"));

			codeNamespace.Types.Add(rootClass);

			var compileUnit = new CodeCompileUnit();
			compileUnit.Namespaces.Add(codeNamespace);
			return GenerateCodeFromCompileUnit(compileUnit);
		}

		private IEnumerable<CodeTypeMember> GenerateMembersRecursive(string parentFullClassName, IEnumerable<Tuple<string[], ResourceItem>> properties, int index = 0)
		{
			foreach (var i in properties.GroupBy(x => x.Item1[index]).OrderBy(x => x.Key))
			{
				var level = index + 1;
				Debug.Assert(!i.Any(x => x.Item1.Length < level));
				var groupProperties = i.Where(x => x.Item1.Length == level);//.OrderBy(x => x.Item1[level]);
				foreach (var j in groupProperties)
				{
					yield return GenerateProperty(j.Item2.Name, j.Item1[index], j.Item2.Value, j.Item2.Comment);
				}

				var subProperties = i.Where(x => x.Item1.Length > level).ToList();
				if (subProperties.Any())
				{
					var subClass = GenerateSubClass(parentFullClassName, i.Key);

					var getStringMethod = GenerateGetStringMethod();
					subClass.Members.Add(getStringMethod);

					var currentFullClassName = string.Join(".", parentFullClassName, subClass.Name);
					var members = GenerateMembersRecursive(currentFullClassName, subProperties, level).ToArray();
					subClass.Members.AddRange(members);

					if (members.Any(x => x.Name == subClass.Name))
					{
						subClass.Comments.Clear();
						subClass.Comments.Add(new CodeCommentStatement("<summary>", true));
						subClass.Comments.Add(new CodeCommentStatement("TODO: Consider to rename this property group as it shares the same name with a property.", true));
						subClass.Comments.Add(new CodeCommentStatement("</summary>", true));
					}

					yield return subClass;
				}
			}
		}

		private IEnumerable<CodeTypeMember> GenerateRootMembers(string rootClassName)
		{
			var propertyGroups = ResourceParser.Parse().Select(x => Tuple.Create(x.Name.Split('.'), x));
			return GenerateMembersRecursive(rootClassName, propertyGroups);
		}

		private CodeTypeDeclaration GenerateRootClass(string className, TypeAttributes attributes)
		{
			var currentClass = new CodeTypeDeclaration
			{
				IsClass = true,
				IsPartial = true,
				Name = className,
				TypeAttributes = attributes,
				Members =
				{
					new CodeTypeConstructor
					{
						Statements =
						{
							new CodeAssignStatement
							{
								Left = new CodeFieldReferenceExpression(null, "_context"),
								Right = new CodeMethodInvokeExpression
								{
									Method = new CodeMethodReferenceExpression(null, "CreateContext")
								}
							},
							new CodeAssignStatement
							{
								Left = new CodeFieldReferenceExpression(null, "_map"),
								Right = new CodeMethodInvokeExpression
								{
									Method = new CodeMethodReferenceExpression(null, "CreateMap",new CodeTypeReference(className))
								}
							}
							
							//TODO: Maybe add partial static methods and call them from within the static ctor
							//static partial void OnStaticConstructorEnter(ref ResourceMap _map, ref ResourceContext _context);
							//static partial void OnStaticConstructorExit(ref ResourceMap _map, ref ResourceContext _context);
						}
					},
				}
			};

			var membersContext = GenerateResourceContextProperty().ToArray();
			currentClass.Members.AddRange(membersContext);

			var membersMap = GenerateResourceMapProperty().ToArray();
			currentClass.Members.AddRange(membersMap);

			var methodCreateContext2 = GenerateCreateContextMethod();
			var methodCreateContext1 = GenerateCreateContextMethod(methodCreateContext2);
			currentClass.Members.Add(methodCreateContext1);
			currentClass.Members.Add(methodCreateContext2);

			var methodCreateMap = GenerateCreateMapMethod();
			currentClass.Members.Add(methodCreateMap);

			//TODO: if Properties.Count>0
			var methodGetString = GenerateGetStringMethod();
			currentClass.Members.Add(methodGetString);

			return currentClass;
		}

		private CodeMemberMethod GenerateCreateContextMethod(CodeTypeMember createContext)
		{
			var resourceContextType = new CodeTypeReference("ResourceContext");
			var dispatcher = new CodeVariableReferenceExpression("dispatcher");
			var window = new CodeVariableReferenceExpression("window");
			var mainView = new CodeVariableReferenceExpression("mainView");

			return new CodeMemberMethod
			{
				Attributes = MemberAttributes.Private | MemberAttributes.Static,
				Name = "CreateContext",
				ReturnType = resourceContextType,
				Statements =
				{
					// var dispatcher = Window.Current?.Dispatcher ?? CoreApplication.MainView?.Dispatcher;
					new CodeVariableDeclarationStatement
					{
						Name = dispatcher.VariableName,
						Type = new CodeTypeReference("var"),
						InitExpression = new CodeDefaultValueExpression(new CodeTypeReference("CoreDispatcher"))
					},
					new CodeVariableDeclarationStatement
					{
						Name = window.VariableName,
						Type = new CodeTypeReference("var"),
						InitExpression = new CodePropertyReferenceExpression
						{
							TargetObject = new CodeTypeReferenceExpression("Window"),
							PropertyName = "Current"
						}
					},
					new CodeConditionStatement
					{
						Condition = new CodeBinaryOperatorExpression
						{
							Left = window,
							Operator = CodeBinaryOperatorType.IdentityInequality,
							Right = new CodePrimitiveExpression(null)
						},
						TrueStatements =
						{
							new CodeAssignStatement
							{
								Left = dispatcher,
								Right = new CodePropertyReferenceExpression
								{
									TargetObject = window,
									PropertyName = "Dispatcher"
								}
							}
						}
					},
					new CodeConditionStatement
					{
						Condition = new CodeBinaryOperatorExpression
						{
							Left = dispatcher,
							Operator = CodeBinaryOperatorType.IdentityEquality,
							Right = new CodePrimitiveExpression(null)
						},
						TrueStatements =
						{
							new CodeVariableDeclarationStatement
							{
								Name = mainView.VariableName,
								Type = new CodeTypeReference("var"),
								InitExpression = new CodePropertyReferenceExpression
								{
									TargetObject = new CodeTypeReferenceExpression("CoreApplication"),
									PropertyName = "MainView"
								}
							},
							new CodeConditionStatement
							{
								Condition = new CodeBinaryOperatorExpression
								{
									Left = mainView,
									Operator = CodeBinaryOperatorType.IdentityInequality,
									Right = new CodePrimitiveExpression(null)
								},
								TrueStatements =
								{
									new CodeAssignStatement
									{
										Left = dispatcher,
										Right = new CodePropertyReferenceExpression
										{
											TargetObject = mainView,
											PropertyName = "Dispatcher"
										}
									}
								}
							}
						}
					},
					// return CreateContext(dispatcher);
					new CodeMethodReturnStatement
					{
						Expression = new CodeMethodInvokeExpression
						{
							Method = new CodeMethodReferenceExpression(null, createContext.Name),
							Parameters = { dispatcher }
						}
					}
				}
			};
		}

		/// <summary>
		/// public static ResourceContext CreateContext(CoreDispatcher dispatcher)
		/// {
		///		if (dispatcher != null)
		///		{
		///			if (dispatcher.HasThreadAccess)
		///			{
		///				return ResourceContext.GetForCurrentView();
		///			}
		///			using (var cancellationTokenSource = new CancellationTokenSource())
		///			{
		///				var context = default(ResourceContext);
		///				var task = dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
		///				{
		///					context = ResourceContext.GetForCurrentView();
		///				}).AsTask(cancellationTokenSource.Token);
		///				// Runs into a 'timeout', when the main-thread is blocked recently. 
		///				// Don't wait too long as there is an alternative way to retrieve an context (for independent use).
		///				task.Wait(TimeSpan.FromSeconds(1));
		///				cancellationTokenSource.Cancel();
		///				if (context != null)
		///					return context;
		///			}
		///		}
		///		return ResourceContext.GetForViewIndependentUse();
		/// }
		/// </summary>
		/// <returns></returns>
		private CodeMemberMethod GenerateCreateContextMethod(MemberAttributes attributes = MemberAttributes.Private | MemberAttributes.Static)
		{
			var dispatcher = new CodeVariableReferenceExpression("dispatcher");
			var resourceContextType = new CodeTypeReference("ResourceContext");
			var resourceContextTypeExpression = new CodeTypeReferenceExpression(resourceContextType);
			var varType = new CodeTypeReference("var");

			//[private static] ResourceContext CreateContext(CoreDispatcher dispatcher)
			return new CodeMemberMethod
			{
				Attributes = attributes,
				Name = "CreateContext",
				Parameters =
				{
					new CodeParameterDeclarationExpression("CoreDispatcher", dispatcher.VariableName)
				},
				ReturnType = resourceContextType,
				Statements =
				{
					new CodeConditionStatement
					{
						// if (dispatcher != null)
						Condition = new CodeBinaryOperatorExpression
						{
							Left = dispatcher,
							Operator = CodeBinaryOperatorType.IdentityInequality,
							Right = new CodePrimitiveExpression(null)
						},
						TrueStatements =
						{
							new CodeConditionStatement
							{
								// if (dispatcher.HasThreadAccess)
								Condition = new CodePropertyReferenceExpression()
								{
									TargetObject = dispatcher,
									PropertyName = "HasThreadAccess"
								},
								TrueStatements =
								{
									// return ResourceContext.GetForCurrentView();
									new CodeMethodReturnStatement
									{
										Expression = new CodeMethodInvokeExpression
										{
											Method = new CodeMethodReferenceExpression
											{
												TargetObject = resourceContextTypeExpression,
												MethodName = "GetForCurrentView"
											}
										}
									}
								}
							},
							// using(var cancellationTokenSource = new CancellationTokenSource()) 
							// Note: But there is no CodeUsingExpression like CodeTypeOfExpression, therefore we use the traditional try/finally-block.
							// var cancellationTokenSource = new CancellationTokenSource()
							new CodeVariableDeclarationStatement
							{
								Type = varType,
								Name = "cancellationTokenSource",
								InitExpression = new CodeObjectCreateExpression(new CodeTypeReference("CancellationTokenSource"))
							},
							new CodeTryCatchFinallyStatement
							{
								TryStatements =
								{
									//var context = default(ResourceContext);
									new CodeVariableDeclarationStatement
									{
											Type = varType,
											Name = "context",
											InitExpression = new CodeDefaultValueExpression(resourceContextType)
									},
									// var action = dispatcher.RunAsync(CoreDispatcherPriority.High, () => context = ResourceContext.GetForCurrentView());
									new CodeVariableDeclarationStatement
									{
										Type = varType,
										Name = "action",
										InitExpression = new CodeMethodInvokeExpression
										{
											Method = new CodeMethodReferenceExpression
											{
												TargetObject = dispatcher,
												MethodName = "RunAsync"
											},
											Parameters =
											{
												new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("CoreDispatcherPriority"), "High"),
												// Note: creating anonymmous delegates is not supported
												new CodeSnippetExpression("() => context = ResourceContext.GetForCurrentView()"),
											}
										}
									},
									// var task = WindowsRuntimeSystemExtensions.AsTask(action, cancellationTokenSource.Token);
									new CodeVariableDeclarationStatement
									{
										Type = varType,
										Name = "task",
										InitExpression = new CodeMethodInvokeExpression
										{
											Method = new CodeMethodReferenceExpression
											{
												TargetObject = new CodeTypeReferenceExpression("WindowsRuntimeSystemExtensions"),
												MethodName = "AsTask"
											},
											Parameters =
											{
												new CodeVariableReferenceExpression("action"),
												new CodePropertyReferenceExpression
												{
													TargetObject = new CodeVariableReferenceExpression("cancellationTokenSource"),
													PropertyName = "Token"
												}
											}
										}
									},
									new CodeCommentStatement("Runs into a 'timeout', when the 'main-thread' is blocked recently."),
									new CodeCommentStatement("Don't wait too long as there is an alternative way to retrieve an context (for independent use)."),
									// task.Wait(TimeSpan.FromSeconds(1));
									new CodeMethodInvokeExpression
									{
										Method = new CodeMethodReferenceExpression
										{
											TargetObject = new CodeVariableReferenceExpression("task"),
											MethodName = "Wait"
										},
										Parameters =
										{
											new CodeMethodInvokeExpression
											{
												Method = new CodeMethodReferenceExpression
												{
													TargetObject = new CodeTypeReferenceExpression("TimeSpan"),
													MethodName = "FromSeconds"
												},
												Parameters =
												{
													new CodePrimitiveExpression(1)
												}
											}
										}
									},
									// cancellationTokenSource.Cancel();
									new CodeMethodInvokeExpression
									{
										Method = new CodeMethodReferenceExpression
										{
											TargetObject = new CodeVariableReferenceExpression("cancellationTokenSource"),
											MethodName = "Cancel"
										}
									},
									new CodeConditionStatement
									{
										// if(context != null)
										Condition = new CodeBinaryOperatorExpression
										{
											Left = new CodeVariableReferenceExpression("context"),
											Operator = CodeBinaryOperatorType.IdentityInequality,
											Right = new CodePrimitiveExpression(null)
										},
										// return context;
										TrueStatements =
										{
											new CodeMethodReturnStatement { Expression = new CodeVariableReferenceExpression("context") }
										}
									}
								},
								FinallyStatements =
								{
									// cancellationTokenSource.Dispose();
									new CodeMethodInvokeExpression
									{
										Method = new CodeMethodReferenceExpression
										{
											TargetObject = new CodeVariableReferenceExpression("cancellationTokenSource"),
											MethodName = "Dispose"
										}
									}
								}
							}
						}
					},
					// return ResourceContext.GetForViewIndependentUse();
					new CodeMethodReturnStatement
					{
						Expression = new CodeMethodInvokeExpression
						{
							Method = new CodeMethodReferenceExpression
							{
								TargetObject = resourceContextTypeExpression,
								MethodName = "GetForViewIndependentUse"
							}
						}
					}
				}
			};
		}

		private CodeMemberMethod GenerateCreateMapMethod(MemberAttributes attributes = MemberAttributes.Public | MemberAttributes.Static)
		{
			var varType = new CodeTypeReference("var");
			var type = new CodeVariableReferenceExpression("type");
			var assemblyName = new CodeVariableReferenceExpression("assemblyName");
			var resourceName = new CodeVariableReferenceExpression("resourceName");

			// [public static] ResourceMap CreateMap<T>()
			return new CodeMemberMethod
			{
				Attributes = attributes,
				Name = "CreateMap",
				ReturnType = new CodeTypeReference("ResourceMap"),
				Statements =
				{
					// var applicationAssemblyName = Application.Current.GetType().GetTypeInfo().Assembly.GetName().Name;
					new CodeVariableDeclarationStatement {
						Type = varType,
						Name = "applicationAssemblyName",
						InitExpression = new CodePropertyReferenceExpression {
							TargetObject = new CodeMethodInvokeExpression {
								Method = new CodeMethodReferenceExpression {
									TargetObject = new CodePropertyReferenceExpression {
										TargetObject = new CodeMethodInvokeExpression
										{
											Method = new CodeMethodReferenceExpression
											{
												TargetObject = new CodeMethodInvokeExpression
												{
													Method = new CodeMethodReferenceExpression
													{
														TargetObject = new CodePropertyReferenceExpression
														{
															TargetObject = new CodeTypeReferenceExpression("Application"),
															PropertyName = "Current"
														},
														MethodName = "GetType"
													},
												},
												MethodName = "GetTypeInfo"
											}
										},
										PropertyName = "Assembly"
									},
									MethodName = "GetName"
								}
							},
							PropertyName = "Name"
						}
					},
					// var type = typeof(T);
					new CodeVariableDeclarationStatement
					{
						Name = type.VariableName,
						Type = varType,
						InitExpression = new CodeTypeOfExpression("T")
					},
					// var assemblyName = type.GetTypeInfo().Assembly.GetName().Name;
					new CodeVariableDeclarationStatement{
						Type = varType,
						Name = assemblyName.VariableName,
						InitExpression = new CodePropertyReferenceExpression
						{
							TargetObject = new CodeMethodInvokeExpression
							{
								Method = new CodeMethodReferenceExpression
								{
									TargetObject = new CodePropertyReferenceExpression
									{
										TargetObject = new CodeMethodInvokeExpression
										{
											Method = new CodeMethodReferenceExpression
											{
												TargetObject = type,
												MethodName = "GetTypeInfo"
											}
										},
										PropertyName = "Assembly"
									},
									MethodName = "GetName"
								}
							},
							PropertyName = "Name"
						}
					},
					// string resourceName;
					new CodeVariableDeclarationStatement
					{
						Name = resourceName.VariableName,
						Type = new CodeTypeReference(typeof(string))
					},
					new CodeConditionStatement
					{
						//if (applicationAssemblyName.Equals(assemblyName))
						Condition = new CodeMethodInvokeExpression
						{
							Method = new CodeMethodReferenceExpression
							{
								TargetObject = new CodeVariableReferenceExpression("applicationAssemblyName"),
								MethodName = "Equals"
							},
							Parameters =
							{
								assemblyName
							}
						},
						// resourceName = type.Name;
						TrueStatements =
						{
							new CodeAssignStatement
							{
								Left = resourceName,
								Right =  new CodePropertyReferenceExpression
								{
									TargetObject = type,
									PropertyName = "Name"
								}
							}
						},
						// resourceName = assemblyName + "/" + type.Name;
						FalseStatements =
						{
							new CodeAssignStatement
							{
								Left = resourceName,
								Right = new CodeBinaryOperatorExpression
								{
									Left = assemblyName,
									Operator = CodeBinaryOperatorType.Add,
									Right = new CodeBinaryOperatorExpression
									{
										Left = new CodePrimitiveExpression("/"),
										Operator = CodeBinaryOperatorType.Add,
										Right = new CodePropertyReferenceExpression
										{
											TargetObject = type,
											PropertyName = "Name"
										}
									}
								}
							}
						}
					},
					// return ResourceManager.Current.MainResourceMap.GetSubtree(resourceName);
					new CodeMethodReturnStatement
					{
						Expression = new CodeMethodInvokeExpression
						{
							Method  = new CodeMethodReferenceExpression
							{
								TargetObject = new CodePropertyReferenceExpression
								{
									TargetObject = new CodePropertyReferenceExpression
									{
										TargetObject = new CodeTypeReferenceExpression("ResourceManager"),
										PropertyName = "Current"
									},
									PropertyName = "MainResourceMap"
								},
								MethodName = "GetSubtree"
							},
							Parameters = { resourceName }
						}
					}
				},
				TypeParameters =
				{
					new CodeTypeParameter
					{
						Name = "T",
						// Workaround: "class" would generate 'T : @class', therefore prepend a space
						Constraints = { " class" }
					}
				}
			};
		}

		private CodeTypeDeclaration GenerateSubClass(string parentClassName, string className, TypeAttributes typeAttributes = TypeAttributes.Public, MemberAttributes memberAttributes = MemberAttributes.Public | MemberAttributes.Static)
		{
			//TODO: add comment?
			var subClass = new CodeTypeDeclaration
			{
				IsClass = true,
				IsPartial = true,
				Name = className,
				TypeAttributes = typeAttributes,
				Members =
				{
					new CodeTypeConstructor
					{
						Statements =
						{
							// _map = {parentClassName}.Map.GetSubtree("{className}");
							new CodeAssignStatement
							{
								Left = new CodeFieldReferenceExpression(null, "_map"),
								Right = GenerateInvokeMethodResourceMapFromSubtree(parentClassName, className)
							}
						}
					}
				}
			};

			var mapMembers = GenerateResourceMapProperty(memberAttributes).ToArray();
			subClass.Members.AddRange(mapMembers);
			return subClass;
		}

		private IEnumerable<CodeTypeMember> GenerateResourceMapProperty(MemberAttributes? additionalAttributes = MemberAttributes.Static)
		{
			var type = new CodeTypeReference("ResourceMap");
			var field = new CodeFieldReferenceExpression(null, "_map");

			// Note: "readonly" is not supported by CodeDom
			var fieldAttributes = MemberAttributes.Private;
			var propertyAttributes = MemberAttributes.Public;
			if (additionalAttributes.HasValue)
			{
				fieldAttributes |= (additionalAttributes.Value & ~MemberAttributes.Public);
				propertyAttributes |= (additionalAttributes.Value & ~MemberAttributes.Private);
			}

			// private ResourceMap _map;
			yield return new CodeMemberField
			{
				Attributes = fieldAttributes,
				Name = field.FieldName,
				Type = type,
			};

			// public ResourceMap Map { get { return _map; } }
			yield return new CodeMemberProperty
			{
				Attributes = propertyAttributes,
				//Comments =
				//{
				//	new CodeCommentStatement("<summary>", true),
				//	new CodeCommentStatement("Holds a cached instance of the referenced ResourceMap.", true),
				//	new CodeCommentStatement("</summary>", true)
				//},
				GetStatements =
				{
					new CodeMethodReturnStatement { Expression = field }
				},
				Name = "Map",
				Type = type
			};
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="additionalAttributes"></param>
		/// <returns></returns>
		private IEnumerable<CodeTypeMember> GenerateResourceContextProperty(MemberAttributes? additionalAttributes = MemberAttributes.Static)
		{
			var type = new CodeTypeReference("ResourceContext");
			var field = new CodeFieldReferenceExpression(null, "_context");
			var valueField = new CodeFieldReferenceExpression(null, "value");

			// Note: "readonly" is not supported by CodeDom
			var fieldAttributes = MemberAttributes.Private;
			var propertyAttributes = MemberAttributes.Public;
			if (additionalAttributes.HasValue)
			{
				fieldAttributes |= (additionalAttributes.Value & ~MemberAttributes.Public);
				propertyAttributes |= (additionalAttributes.Value & ~MemberAttributes.Private);
			}

			// private ResourceContext _context;
			yield return new CodeMemberField
			{
				Attributes = fieldAttributes,
				Name = field.FieldName,
				Type = type
			};

			// public ResourceContext Context { get { return _context; } set { _context = value; } }
			yield return new CodeMemberProperty
			{
				Attributes = propertyAttributes,
				Comments =
				{
					new CodeCommentStatement("<remarks>", true),
					new CodeCommentStatement("This property is prefilled with the default ResourceContext instance, which is shared for all resources (and views).", true),
					new CodeCommentStatement("When you want to modify the ResourceContext, you should consider to clone the existing ResourceContext before.", true),
					new CodeCommentStatement("</remarks>", true)
				},
				GetStatements =
				{
					new CodeMethodReturnStatement { Expression = field }
				},
				SetStatements =
				{
					new CodeConditionStatement
					{
						Condition = new CodeBinaryOperatorExpression
						{
							Left = valueField,
							Operator = CodeBinaryOperatorType.IdentityEquality,
							Right = new CodePrimitiveExpression(null)
						},
						TrueStatements =
						{
							new CodeThrowExceptionStatement
							{
								ToThrow = new CodeObjectCreateExpression(new CodeTypeReference(typeof(ArgumentNullException)), new CodePrimitiveExpression(valueField.FieldName), new CodePrimitiveExpression("Provide a valid ResourceContext."))
							}
						}
					},
					new CodeAssignStatement
					{
						Left = new CodeFieldReferenceExpression(null, field.FieldName),
						Right = valueField
					}
				},
				Name = "Context",
				Type = type
			};
		}

		private CodeMemberProperty GenerateProperty(string propertyName, string propertyNameLastSegment, string propertyValue, string propertyComment, MemberAttributes attributes = MemberAttributes.Public | MemberAttributes.Static)
		{
			var property = new CodeMemberProperty
			{
				Attributes = attributes,
				GetStatements =
				{
					new CodeMethodReturnStatement
					{
						Expression = new CodeMethodInvokeExpression
						{
							Method = new CodeMethodReferenceExpression(null, "GetString"),
							Parameters = { new CodePrimitiveExpression(propertyNameLastSegment) }
						}
					}
				},
				Name = propertyNameLastSegment,
				Type = new CodeTypeReference(typeof(string))
			};
			var name = propertyName != propertyNameLastSegment ? propertyName : null;
			var comments = GeneratePropertyComment(name, propertyValue, propertyComment);
			property.Comments.AddRange(comments.ToArray());
			return property;
		}

		private IEnumerable<CodeCommentStatement> GeneratePropertyComment(string propertyName, string propertyValue, string propertyComment)
		{
			if (!string.IsNullOrWhiteSpace(propertyComment))
			{
				yield return new CodeCommentStatement("<summary>", true);
				yield return new CodeCommentStatement(EscapeComment(propertyComment), true);
				yield return new CodeCommentStatement("</summary>", true);
			}
			if (!string.IsNullOrEmpty(propertyName))
			{
				yield return new CodeCommentStatement("<remarks>", true);
				yield return new CodeCommentStatement("Represents resource " + EscapeComment(propertyName), true);
				yield return new CodeCommentStatement("</remarks>", true);
			}
			if (!string.IsNullOrWhiteSpace(propertyValue))
			{
				yield return new CodeCommentStatement("<example>", true);
				yield return new CodeCommentStatement(EscapeComment(propertyValue), true);
				yield return new CodeCommentStatement("</example>", true);
			}
		}

		private static string EscapeComment(string value)
		{
			value = value.Replace("<", "&lt;");
			value = value.Replace(">", "&gt;");
			value = value.Replace("&", "&amp;");
			return value;
		}

		private CodeMemberMethod GenerateGetStringMethod(MemberAttributes attributes = MemberAttributes.Private | MemberAttributes.Static)
		{
			var parameter = new CodeParameterDeclarationExpression(typeof(string), "propertyName");
			return new CodeMemberMethod
			{
				Attributes = attributes,
				//Comments =
				//{
				//	new CodeCommentStatement("<summary>", true),
				//	new CodeCommentStatement("Retrieve a single value from the resource file as string.", true),
				//	new CodeCommentStatement("</summary>", true)
				//},
				Name = "GetString",
				Parameters = { parameter },
				ReturnType = new CodeTypeReference(typeof(string)),
				Statements =
				{
					new CodeMethodReturnStatement
					{
						Expression = new CodePropertyReferenceExpression
						{
							TargetObject = new CodeMethodInvokeExpression
							{
								Method = new CodeMethodReferenceExpression
								{
									TargetObject = new CodePropertyReferenceExpression(null, "Map"),
									MethodName = "GetValue"
								},
								Parameters =
								{
									new CodeVariableReferenceExpression(parameter.Name),
									new CodePropertyReferenceExpression(null, "Context")
								}
							},
							PropertyName = "ValueAsString"
						}
					}
				}
			};
		}

		private CodeMethodInvokeExpression GenerateInvokeMethodResourceMapFromSubtree(string parentClassName, string subtreeName)
		{
			return new CodeMethodInvokeExpression
			{
				Method = new CodeMethodReferenceExpression
				{
					TargetObject = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(parentClassName), "Map"),
					MethodName = "GetSubtree"
				},
				Parameters = { new CodePrimitiveExpression(subtreeName) }
			};
		}

		private string GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit)
		{
			var options = new CodeGeneratorOptions { BracingStyle = "C" };

			var buffer = new StringBuilder();
			using (var writer = new StringWriter(buffer))
				_provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);

			return buffer.ToString();
		}

		private bool _disposed;

		public void Dispose()
		{
			Dispose(true);
		}

		~CodeDomCodeGenerator()
		{
			Dispose(false);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed)
				return;
			_disposed = true;
			if (disposing)
				_provider.Dispose();
		}
	}
}